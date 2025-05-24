#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using FamilyTreeApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// Build configuration
var builder = Host.CreateDefaultBuilder(args);

// Configure services
builder.ConfigureServices(
    (context, services) =>
    {
        // Add configuration for OpenAI API key
        var configuration = context.Configuration;

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
Console.WriteLine("- Get details for person p5");
Console.WriteLine();

var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage(
    @"You are a helpful assistant that can answer questions about a family tree.
You have access to family tools that can get information about people and their relationships.
When users ask about the family, use the available tools to get the information.
Be conversational and helpful in your responses."
);

while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "exit")
    {
        break;
    }

    chatHistory.AddUserMessage(userInput);

    try
    {
        Console.Write("Assistant is thinking...");

        // Enable auto function calling
        OpenAIPromptExecutionSettings executionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel
        );

        Console.Write("\rAssistant: ");
        Console.WriteLine(response.Content);
        chatHistory.AddAssistantMessage(response.Content ?? "");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine();
}

Console.WriteLine("Goodbye!");
