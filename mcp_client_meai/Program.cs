using System.ComponentModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;

Console.WriteLine("FamilyTools MCP Chat Client with OpenAI .NET SDK (type 'exit' to quit)");

// Get OpenAI API key
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

// Start MCP server (same as your original code)
var mcpLibraryProject = Path.Combine(
    AppContext.BaseDirectory,
    "..",
    "..",
    "..",
    "..",
    "mcp_library",
    "mcp_library.csproj"
);
if (!File.Exists(mcpLibraryProject))
{
    Console.WriteLine($"MCP library project not found at {mcpLibraryProject}.");
    return;
}

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

mcpProcess.Start();
var mcpInput = mcpProcess.StandardInput;
var mcpOutput = mcpProcess.StandardOutput;

// Background task for stderr
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

await Task.Delay(10_000); // Wait for MCP server to start

// Create OpenAI client
var openAIClient = new OpenAIClient(apiKey);
var chatClient = openAIClient.GetChatClient("gpt-4o");

// Define tools
var tools = new List<ChatTool>
{
    ChatTool.CreateFunctionTool(
        functionName: "GetFamily",
        functionDescription: "Get a list of people in a family.",
        functionParameters: BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
    ),
    ChatTool.CreateFunctionTool(
        functionName: "GetPerson",
        functionDescription: "Get a member of the family by id.",
        functionParameters: BinaryData.FromString(
            "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\",\"description\":\"The ID of the person to retrieve.\"}},\"required\":[\"id\"]}"
        )
    ),
};

var chatOptions = new ChatCompletionOptions { Tools = tools };

// Chat history
var chatHistory = new List<ChatMessage>
{
    new SystemChatMessage(
        "You are a helpful assistant with access to a family tree via MCP tools. Use the available tools to answer questions about the family."
    ),
};

// Chat loop
while (true)
{
    Console.Write("User: ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    chatHistory.Add(new UserChatMessage(userInput));

    try
    {
        var response = await chatClient.CompleteChatAsync(chatHistory, chatOptions);
        var message = response.Value.Content[0];

        // Check if there are tool calls
        if (response.Value.ToolCalls?.Count > 0)
        {
            // Process tool calls
            foreach (var toolCall in response.Value.ToolCalls)
            {
                if (toolCall is ChatToolCall functionCall)
                {
                    var functionName = functionCall.FunctionName;
                    var functionArgs = functionCall.FunctionArguments;

                    // Call MCP tool
                    var mcpResult = await CallMcpTool(functionName, functionArgs);

                    // Add tool result to chat history
                    chatHistory.Add(new ToolChatMessage(toolCall.Id, mcpResult));
                }
            }

            // Add the assistant message with tool calls
            chatHistory.Add(new AssistantChatMessage(response.Value));

            // Get final response with tool results
            var finalResponse = await chatClient.CompleteChatAsync(chatHistory, chatOptions);
            Console.WriteLine($"Assistant: {finalResponse.Value.Content[0].Text}");
            chatHistory.Add(new AssistantChatMessage(finalResponse.Value));
        }
        else
        {
            Console.WriteLine($"Assistant: {message.Text}");
            chatHistory.Add(new AssistantChatMessage(response.Value));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
}

async Task<string> CallMcpTool(string method, string args)
{
    var mcpRequest = JsonSerializer.Serialize(
        new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method,
            @params = JsonSerializer.Deserialize<JsonElement>(args),
        }
    );

    await mcpInput.WriteLineAsync(mcpRequest);
    await mcpInput.FlushAsync();

    string mcpResult = await mcpOutput.ReadLineAsync() ?? "";

    try
    {
        using var mcpJson = JsonDocument.Parse(mcpResult);
        if (mcpJson.RootElement.TryGetProperty("result", out var resultProp))
        {
            return resultProp.GetRawText();
        }
        else if (mcpJson.RootElement.TryGetProperty("error", out var errorProp))
        {
            return errorProp.GetRawText();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MCP PARSE ERROR] {ex.Message}");
    }

    return mcpResult;
}

// Cleanup
mcpProcess.Kill();
