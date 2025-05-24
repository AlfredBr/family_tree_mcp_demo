using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.AI;
//using Microsoft.Extensions.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using FamilyTreeApp;

// Build configuration
var builder = Host.CreateDefaultBuilder(args);

// Configure logging
builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.ColorBehavior = LoggerColorBehavior.Enabled;
        options.SingleLine = true;
    });
});

// Configure services
builder.ConfigureServices(
    (context, services) =>
    {
        // Add logging
        services.AddLogging();

        // Get OpenAI API key
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.Write("Enter your OpenAI API key: ");
            apiKey = Console.ReadLine();
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("API key is required.");
        }

        // Register ChatClient using Microsoft.Extensions.AI.OpenAI
        services.AddSingleton<IChatClient>(
            provider => new OpenAI.Chat.ChatClient("gpt-4o", apiKey).AsIChatClient()
        );
    }
);

// Build the host
var host = builder.Build();

// get logger
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Start MCP server
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
logger.LogInformation("MCP server started.");

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

// Get ChatClient from DI
var chatClient = host.Services.GetRequiredService<IChatClient>();

Console.WriteLine("=== Family Tree ChatBot with GPT-4o ===");
Console.WriteLine("Ask me about family members, relationships, or type 'quit' to exit.");
Console.WriteLine();

var prePromptInstructions = new List<string>
{
    "You are a helpful assistant that can answer questions about a family tree.",
    "You have access to family tools that can get information about people and their relationships.",
    "When users ask about the family, use the available tools to get the information.",
    "Be conversational and helpful in your responses.",
    "Do not use Markdown notation in your responses.",
    "When you give your answer, provide a summary of how you determined that answer.",
    "Double check your answers before responding.  Assume that you have made a mistake and you need to verify your response.",
    $"Today's date is {DateTime.Today}.",
};


// Initialize conversation history using proper Microsoft.Extensions.AI types
var conversation = new List<ChatMessage>
{
    new(
        ChatRole.System,
        string.Join(" ", prePromptInstructions)
    ),
};

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
    {
        break;
    }

    try
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Assistant: ");
        Console.ResetColor();

        if (
            userInput.Contains("family", StringComparison.CurrentCultureIgnoreCase)
            || userInput.Contains("member", StringComparison.CurrentCultureIgnoreCase)
            || userInput.Contains("list", StringComparison.CurrentCultureIgnoreCase)
        )
        {
            // Call MCP server to get family data
            var familyData = await CallMcpTool(mcpInput, mcpOutput, "GetFamily", new object[] { new { } });
            if (!string.IsNullOrEmpty(familyData))
            {
                Console.WriteLine($"Here's the family data: {familyData}");
            }
            else
            {
                Console.WriteLine("I couldn't retrieve the family data at the moment.");
            }
        }
        else
        {
            Console.WriteLine(
                "I'm a family tree assistant. Ask me about family members, relationships, or say 'list family' to see all members!"
            );
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Sorry, I encountered an error: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}

Console.WriteLine("Goodbye!");

// Cleanup
try
{
    if (!mcpProcess.HasExited)
    {
        mcpProcess.Kill();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error while cleaning up MCP process: {ex.Message}");
}

// Helper method to call MCP tools
async Task<string?> CallMcpTool(
    StreamWriter input,
    StreamReader output,
    string toolName,
    object[] parameters
)
{
    try
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments = parameters.Length > 0 ? parameters[0] : new { },
            },
        };

        var requestJson = JsonSerializer.Serialize(request);
        await input.WriteLineAsync(requestJson);
        await input.FlushAsync();

        var responseJson = await output.ReadLineAsync();
        if (!string.IsNullOrEmpty(responseJson))
        {
            var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
            if (
                response.TryGetProperty("result", out var result)
                && result.TryGetProperty("content", out var content)
            )
            {
                return content.GetString();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to call MCP tool {toolName}: {ex.Message}");
    }

    return null;
}
