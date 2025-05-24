#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using FamilyTreeApp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// Build configuration
var builder = Host.CreateDefaultBuilder(args);

// Configure logging
builder.ConfigureLogging(logging =>
{
	logging.ClearProviders();
	logging.AddSimpleConsole(
		options =>
		{
			//options.IncludeScopes = true;
			//options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
			options.ColorBehavior = LoggerColorBehavior.Enabled; // Corrected namespace usage
			options.SingleLine = true;
		}
	);
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
		services.AddOpenAIChatCompletion("gpt-4o", openAIApiKey);
	}
);

var host = builder.Build();

// Get services
var kernel = host.Services.GetRequiredService<Kernel>();
var familyService = host.Services.GetRequiredService<FamilyService>();

// Add the family tools as plugins to the kernel
kernel.Plugins.AddFromObject(new FamilyToolsPlugin(familyService), "FamilyTools");

// Get chat completion service
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("🌳 Family Tree Chatbot powered by GPT-4o");
Console.WriteLine("Ask me anything about the family tree!");
Console.WriteLine("Type 'exit' to quit.");
Console.WriteLine("Examples:");
Console.WriteLine("- List all people in the family");
Console.WriteLine("- Who are the parents of Elizabeth Carter?");
Console.WriteLine("- What is the relationship between Emily Smith and William Carter?");
Console.WriteLine("- Get details for person p5.");
Console.WriteLine();

// Define the system message as a list of strings
var prePromptInstructions = new List<string>
{
	"You are a helpful assistant that can answer questions about a family tree.",
	"You have access to family tools that can get information about people and their relationships.",
	"When users ask about the family, use the available tools to get the information.",
	"Be conversational and helpful in your responses.",
	"Do not use Markdown notation in your responses.",
	"When you give your answer, provide a summary of how you determined that answer.",
	"Double check your answers before responding.  Assume that you have made a mistake and you need to verify your response.",
	$"Today's date is {DateTime.Today}."
};

// Create a new chat history
var chatHistory = new ChatHistory();

// add the prePromptInstructions to the chat history as a system message
chatHistory.AddSystemMessage(string.Join(" ", prePromptInstructions));

// begin the chat loop
while (true)
{
	Console.Write("You: ");
	var userInput = Console.ReadLine();

	// Check for exit command
	if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
	{
		break;
	}

	// Add user message to chat history
	chatHistory.AddUserMessage(userInput);

	try
	{
		Console.WriteLine("Assistant is thinking...");

		// Enable auto function calling (i.e. tool use)
		OpenAIPromptExecutionSettings executionSettings = new()
		{
			FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
		};

		// Get the chat message content asynchronously
		var response = await chatCompletionService.GetChatMessageContentAsync(
			chatHistory,
			executionSettings,
			kernel
		);

		// Display the assistant's response
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.Write("\nAssistant: ");
		Console.WriteLine(response.Content);
		Console.ResetColor();
		chatHistory.AddAssistantMessage(response.Content ?? "");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}

	Console.WriteLine();
}

Console.WriteLine("Goodbye!");
