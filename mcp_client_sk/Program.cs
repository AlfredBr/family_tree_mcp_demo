#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable S125

using FamilyTreeApp;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// Build configuration
var builder = Host.CreateDefaultBuilder(args);

// Configure logging
builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        //options.IncludeScopes = true;
        //options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        options.ColorBehavior = LoggerColorBehavior.Enabled; // Corrected namespace usage
        options.SingleLine = true;
    });
});

// Configure services
builder.ConfigureServices(
    (context, services) =>
    {
        // Add configuration for OpenAI API key
        //var configuration = context.Configuration;

        // Add FamilyService
        services.AddSingleton<FamilyService>();

        // Add Semantic Kernel
        services.AddKernel();

        // Get OpenAI API key from environment variable
        var openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(openAIApiKey))
        {
            // [System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "my_api_key_value_from_https://platform.openai.com/settings/organization/api-keys", "User")
            throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
        }

        // Configure OpenAI
        services.AddOpenAIChatCompletion("gpt-4o-mini", openAIApiKey);
    }
);

var host = builder.Build();

// Get the required services
var kernel = host.Services.GetRequiredService<Kernel>();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
var familyService = host.Services.GetRequiredService<FamilyService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Add the family tools as plugins to the kernel
kernel.Plugins.AddFromObject(new FamilyToolsPlugin(familyService), "FamilyTools");
logger.LogInformation("Family tools plugin loaded.");

// Display welcome message
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

// Create a new chat history
var chatHistory = new ChatHistory();

// add the prePromptInstructions to the chat history as a system message
chatHistory.AddSystemMessage(string.Join(" ", Prompt.PrePromptInstructions));

// begin the chat loop
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    var userInput = Console.ReadLine();

	// Check for exit command
	if (string.IsNullOrWhiteSpace(userInput) || new[] { "quit", "exit" }.Any(command => string.Equals(command, userInput, StringComparison.CurrentCultureIgnoreCase)))
	{
		logger.LogInformation("Exiting chat loop.");
        break;
    }

    // Add user message to chat history
    chatHistory.AddUserMessage(userInput);

    try
    {
        Console.WriteLine("Assistant is thinking...");

        // Enable auto function calling (i.e. tool use)
        var executionSettings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

        // Get the chat message content asynchronously
        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);

        // Display the assistant's response
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("\nAssistant: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(response.Content);
        Console.ResetColor();
        Console.WriteLine();

        // Add the assistant's response to the chat history
        if (response.Content != null)
        {
            chatHistory.AddAssistantMessage(response.Content);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}

host.Dispose();
Console.WriteLine("Goodbye!");
