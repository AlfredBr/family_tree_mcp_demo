using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

Console.WriteLine("FamilyTools MCP Chat Client (type 'exit' to quit)");

// Prompt for OpenAI API key if not set in environment
string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Write("Enter your OpenAI API key: ");
    apiKey = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("API key is required.");
    return;
}

// path to the MCP library project
var mcpLibraryProject = Path.Combine(
    AppContext.BaseDirectory,
    "..",
    "..",
    "..",
    "..",
    "mcp_library",
    "mcp_library.csproj"
);

// Check if the MCP library project exists
if (!File.Exists(mcpLibraryProject))
{
    Console.WriteLine($"MCP library project not found at {mcpLibraryProject}.");
    return;
}

// MCP server process info
var mcpProcess = new System.Diagnostics.Process
{
    StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{mcpLibraryProject}\"",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = AppContext.BaseDirectory,
    },
};

// Start the MCP server process
var isRunning = mcpProcess.Start();

// Check if the process started successfully
if (!isRunning)
{
    Console.WriteLine("Failed to start MCP process.");
    return;
}

// get the input and output streams for the MCP server process
var mcpInput = mcpProcess.StandardInput;
var mcpOutput = mcpProcess.StandardOutput;

// Start a background task to print MCP server stderr
_ = Task.Run(async () =>
{
    string? line;
    while ((line = await mcpProcess.StandardError.ReadLineAsync()) != null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[MCP STDERR] {line}");
        Console.ResetColor();
    }
});

// Give MCP server time to start (may need to compile the project)
await Task.Delay(10_000);

// create an HTTP client for OpenAI API requests
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

// this is the system prompt for the assistant
string systemPrompt =
    "You are a helpful assistant with access to a family tree via MCP tools. "
    + "Use the available tools to answer questions about the family.";

// initialize the chat history with the system prompt
var chatHistory = new List<Dictionary<string, object>>
{
    new() { ["role"] = "system", ["content"] = systemPrompt },
};

// begin the chat loop between the user and the assistant
while (true)
{
    Console.Write("User: ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Trim().ToLower() == "exit")
    {
        break;
    }

    chatHistory.Add(new() { ["role"] = "user", ["content"] = userInput });

    // create the request body for the OpenAI API and send the request to the API
    var requestBody = new
    {
        model = "gpt-4o",
        messages = chatHistory,
        tools = new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "GetFamily",
                    description = "Get a list of people in a family.",
                    parameters = new { type = "object", properties = new { } },
                },
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetPerson",
                    description = "Get a member of the family by id.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new
                            {
                                type = "string",
                                description = "The ID of the person to retrieve.",
                            },
                        },
                        required = new[] { "id" },
                    },
                },
            },
        },
        tool_choice = "auto",
    };

    var content = new StringContent(
        JsonSerializer.Serialize(requestBody),
        Encoding.UTF8,
        "application/json"
    );

    var response = await httpClient.PostAsync(
        "https://api.openai.com/v1/chat/completions",
        content
    );

    var responseString = await response.Content.ReadAsStringAsync();

    // Parse response and handle tool calls if present
    using var doc = JsonDocument.Parse(responseString);
    var root = doc.RootElement;
    var choices = root.GetProperty("choices");
    var message = choices[0].GetProperty("message");
    if (message.TryGetProperty("tool_calls", out var toolCalls))
    {
        // Prepare tool results to add after all tool calls are processed
        var toolResults = new List<Dictionary<string, object>>();
        foreach (var toolCall in toolCalls.EnumerateArray())
        {
            var function = toolCall.GetProperty("function");
            var name = function.GetProperty("name").GetString()!;
            var arguments = function.GetProperty("arguments").GetRawText();
            // Compose full JSON-RPC 2.0 message
            var mcpRequest = JsonSerializer.Serialize(
                new
                {
                    jsonrpc = "2.0",
                    id = Guid.NewGuid().ToString(),
                    method = name,
                    @params = JsonSerializer.Deserialize<JsonElement>(arguments),
                }
            );
            mcpInput.WriteLine(mcpRequest);
            mcpInput.Flush();
            // Read response from MCP server
            string mcpResult = await mcpOutput.ReadLineAsync() ?? "";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[MCP RESPONSE RAW] {mcpResult}");
            Console.ResetColor();
            string toolContent = mcpResult;
            try
            {
                using var mcpJson = JsonDocument.Parse(mcpResult);
                if (mcpJson.RootElement.TryGetProperty("result", out var resultProp))
                {
                    toolContent = resultProp.GetRawText();
                }
                else if (mcpJson.RootElement.TryGetProperty("error", out var errorProp))
                {
                    toolContent = errorProp.GetRawText();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[MCP RESPONSE PARSE ERROR] {ex.Message}");
                Console.ResetColor();
            }
            // Prepare tool result (do not add to chatHistory yet)
            toolResults.Add(
                new Dictionary<string, object>
                {
                    { "role", "tool" },
                    { "tool_call_id", toolCall.GetProperty("id").GetString()! },
                    { "name", name },
                    { "content", toolContent },
                }
            );
        }
        // Add all tool results to chatHistory at once
        chatHistory.AddRange(toolResults);
        // Re-ask OpenAI for a final answer with tool results
        var followupContent = new StringContent(
            JsonSerializer.Serialize(new { model = "gpt-4o", messages = chatHistory }),
            Encoding.UTF8,
            "application/json"
        );
        var followupResponse = await httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            followupContent
        );
        var followupString = await followupResponse.Content.ReadAsStringAsync();
        using var followupDoc = JsonDocument.Parse(followupString);
        var followupChoices = followupDoc.RootElement.GetProperty("choices");
        var followupMessage = followupChoices[0].GetProperty("message");
        var finalContent = followupMessage.GetProperty("content").GetString()!;
        Console.WriteLine($"Assistant: {finalContent}\n");
        chatHistory.Add(new() { ["role"] = "assistant", ["content"] = finalContent });
    }
    else
    {
        var contentText = message.GetProperty("content").GetString()!;
        Console.WriteLine($"Assistant: {contentText}\n");
        chatHistory.Add(new() { ["role"] = "assistant", ["content"] = contentText });
    }
}

// Cleanup
mcpProcess.Kill();
