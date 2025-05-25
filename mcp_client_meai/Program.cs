using FamilyTreeApp;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using OpenAI;

using System.Diagnostics;
using System.Text.Json;

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
        services.AddSingleton<IChatClient>(provider => new OpenAI.Chat.ChatClient("gpt-4o", apiKey).AsIChatClient());
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

var mcpProcess = new Process
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
logger.LogInformation("MCP server started.");

var mcpInput = mcpProcess.StandardInput;
var mcpOutput = mcpProcess.StandardOutput;

// Background task for stderr
_ = Task.Run(async () =>
{
    string? line;
    while ((line = await mcpProcess.StandardError.ReadLineAsync()) != null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[MCP STDERR] {line}");
        Console.ResetColor();
    }
});

await Task.Delay(10_000); // Wait for MCP server to start

// Get ChatClient from DI
var openAiClient = host.Services.GetRequiredService<IChatClient>();
var chatClient = new ChatClientBuilder(openAiClient).UseFunctionInvocation().Build();
var chatOptions = new ChatOptions { Tools = [AIFunctionFactory.Create(FamilyTools.GetFamily)] };

Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("=== Family Tree ChatBot with GPT-4o ===");
Console.WriteLine("Ask me about family members, relationships, or type 'quit' to exit.");
Console.WriteLine();
Console.ResetColor();

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

// Initialize conversation history using Microsoft.Extensions.AI.CharMessage type
var conversation = new List<ChatMessage> { new(ChatRole.System, string.Join(" ", prePromptInstructions)) };

// Start the chat loop
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
    {
        // Exit the chat loop
        logger.LogInformation("Exiting chat loop.");
        break;
    }

    try
    {
        conversation.Add(new ChatMessage(ChatRole.User, userInput));

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Assistant: ");

        Console.ForegroundColor = ConsoleColor.White;

        await foreach (var message in chatClient.GetStreamingResponseAsync(conversation, chatOptions))
        {
            Console.Write(message);

            if (message.Role == ChatRole.Assistant)
            {
                conversation.Add(new ChatMessage(ChatRole.Assistant, message.Contents));
            }
        }

        Console.ResetColor();

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

