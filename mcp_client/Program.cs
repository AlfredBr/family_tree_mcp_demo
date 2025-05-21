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

// MCP server process info
var mcpProcess = new System.Diagnostics.Process
{
    StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "run --project ../mcp/mcp.csproj",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = AppContext.BaseDirectory,
    }
};
mcpProcess.Start();
var mcpInput = mcpProcess.StandardInput;
var mcpOutput = mcpProcess.StandardOutput;

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

string systemPrompt = "You are a helpful assistant with access to a family tree via MCP tools. Use the available tools to answer questions about the family.";

var chatHistory = new List<Dictionary<string, object>>
{
    new() { ["role"] = "system", ["content"] = systemPrompt },
};

while (true)
{
    Console.Write("You: ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Trim().ToLower() == "exit")
        break;

    chatHistory.Add(new() { ["role"] = "user", ["content"] = userInput });

    var requestBody = new
    {
        model = "gpt-4o",
        messages = chatHistory,
        tools = new[]
        {
            new {
                type = "function",
                function = new {
                    name = "GetFamily",
                    description = "Get a list of people in a family.",
                    parameters = new { },
                },
            },
            new {
                type = "function",
                function = new {
                    name = "GetPerson",
                    description = "Get a member of the family by id.",
                    parameters = new { id = "string" },
                },
            },
        },
        tool_choice = "auto"
    };

    var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
    var responseString = await response.Content.ReadAsStringAsync();

    // Parse response and handle tool calls if present
    using var doc = JsonDocument.Parse(responseString);
    var root = doc.RootElement;
    var choices = root.GetProperty("choices");
    var message = choices[0].GetProperty("message");
    if (message.TryGetProperty("tool_calls", out var toolCalls))
    {
        foreach (var toolCall in toolCalls.EnumerateArray())
        {
            var function = toolCall.GetProperty("function");
            var name = function.GetProperty("name").GetString();
            var arguments = function.GetProperty("arguments").GetRawText();
            // Forward tool call to MCP server
            mcpInput.WriteLine(arguments); // This is a placeholder; actual MCP protocol may differ
            mcpInput.Flush();
            // Read response from MCP server
            string mcpResult = await mcpOutput.ReadLineAsync() ?? "";
            // Add tool result to chat history
            chatHistory.Add(
                new Dictionary<string, object>
                {
                    { "role", "tool" },
                    { "tool_call_id", toolCall.GetProperty("id").GetString() },
                    { "name", name },
                    { "content", mcpResult },
                }
            );
        }
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
        var finalContent = followupMessage.GetProperty("content").GetString();
        Console.WriteLine($"Assistant: {finalContent}\n");
        chatHistory.Add(new() { ["role"] = "assistant", ["content"] = finalContent });
    }
    else
    {
        var contentText = message.GetProperty("content").GetString();
        Console.WriteLine($"Assistant: {contentText}\n");
        chatHistory.Add(new() { ["role"] = "assistant", ["content"] = contentText });
    }
}

// Cleanup
mcpProcess.Kill();
