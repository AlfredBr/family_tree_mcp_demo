using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using System.Text;

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

		services.AddSingleton<FamilyService>();

		// Register ChatClient using Microsoft.Extensions.AI.OpenAI
		services.AddSingleton<IChatClient>(provider => new OpenAI.Chat.ChatClient("gpt-4o-mini", apiKey).AsIChatClient());
    }
);

// Build the host
var host = builder.Build();

Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("🌳 Family Tree Chatbot powered by GPT-4o-mini");
Console.WriteLine("Ask me anything about the family tree!");
Console.WriteLine("Type 'exit' to quit.");
Console.WriteLine();
Console.WriteLine("Examples:");
Console.WriteLine("- List all people in the family");
Console.WriteLine("- Who are the parents of Elizabeth Carter?");
Console.WriteLine("- What is the relationship between Emily Smith and William Carter?");
Console.WriteLine("- Get details for person p5.");
Console.WriteLine();
Console.ResetColor();

var prePromptInstructions = new List<string>
{
    "You are a helpful assistant that can answer questions about a family tree.",
    "You have access to the GetFamily and GetPerson tools that can get information about people and their relationships.",
    "When users ask about the family, use the available GetFamily and GetPerson tools to get the information.",
    "Be conversational and helpful in your responses.",
    "Do not use Markdown notation in your responses.",
    "When you give your answer, provide a summary of how you determined that answer.",
    "Double check your answers before responding.  Assume that you have made a mistake and you need to verify your response.",
    $"Today's date is {DateTime.Today}.",
};

// Get logger
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Get ChatClient from DI
var openAiClient = host.Services.GetRequiredService<IChatClient>();
var chatClient = new ChatClientBuilder(openAiClient).UseFunctionInvocation().Build();
var familyService = host.Services.GetRequiredService<FamilyService>();
Task<string> GetFamilyAsync() => FamilyTools.GetFamily(familyService);
Task<string?> GetPersonAsync(string id) => FamilyTools.GetPerson(familyService, id);

var chatOptions = new ChatOptions {
    Tools =
    [
        AIFunctionFactory.Create(GetFamilyAsync),
        AIFunctionFactory.Create((string id) => GetPersonAsync(id))
    ]
};

// Initialize conversation history using Microsoft.Extensions.AI.CharMessage type
var conversation = new List<ChatMessage> { new(ChatRole.System, string.Join(" ", prePromptInstructions)) };

// Start the chat loop
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    // Read user input
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput) || new[] { "quit", "exit" }.Any(command => string.Equals(command, userInput, StringComparison.CurrentCultureIgnoreCase)))
    {
        // Exit the chat loop
        logger.LogInformation("Exiting chat loop.");
        break;
    }

    try
    {
        conversation.Add(new ChatMessage(ChatRole.User, userInput));

        Console.WriteLine("Assistant is thinking...");

        // Set a 60 seconds timeout for cancelation
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(60));

		// Capture the response text
		var responseText = new StringBuilder();

        await foreach (var messageUpdate in chatClient.GetStreamingResponseAsync(conversation, chatOptions, cts.Token))
        {
            if (messageUpdate.Role == ChatRole.Assistant)
            {
                conversation.Add(new ChatMessage(ChatRole.Assistant, messageUpdate.Text));
                responseText.Append(messageUpdate.Text);
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("\nAssistant: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(responseText.ToString());
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
