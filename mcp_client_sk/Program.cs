#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable S125

// https://platform.openai.com/docs/guides/function-calling?api-mode=responses
// https://www.youtube.com/watch?v=FLpS7OfD5-s

using FamilyTreeApp;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var llmModel = "o4-mini";

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
		services.AddOpenAIChatCompletion(llmModel, openAIApiKey);
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
logger.LogInformation("FamilyTools plugin loaded.");

// Display welcome message
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine($"🌳 Family Tree Chatbot powered by {llmModel}");
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

// Add the pre-prompt instructions to the chat history as a system message
chatHistory.AddSystemMessage(string.Join(" ", Prompt.PrePromptInstructions));

// Begin the chat loop
while (true)
{
	Console.ForegroundColor = ConsoleColor.Green;
	Console.Write("You: ");
	Console.ResetColor();

	// Read user input message
	var userInput = Console.ReadLine();

	// Check for exit command
	if (string.IsNullOrWhiteSpace(userInput))
	{
		continue;
	}
	if (string.Equals("exit", userInput, StringComparison.CurrentCultureIgnoreCase) ||
		string.Equals("quit", userInput, StringComparison.CurrentCultureIgnoreCase))
	{
		logger.LogInformation("Exiting chat loop.");
		break;
	}

	// Add user message to chat history
	chatHistory.AddUserMessage(userInput);

	try
	{
		Console.ForegroundColor = ConsoleColor.DarkGray;
		Console.WriteLine("Assistant is thinking...");
		Console.ResetColor();

		// Enable auto function calling (i.e. tool use)
		var executionSettings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

		// Get the chat message content asynchronously
		var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);

		if (response?.Content?.Length > 0)
		{
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
