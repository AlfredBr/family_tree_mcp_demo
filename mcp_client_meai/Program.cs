using System.Text;
using FamilyTreeApp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

// Build configuration
var builder = Host.CreateDefaultBuilder(args);

var llmModel = "o4-mini";

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
            // [System.Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "my_api_key_value_from_https://platform.openai.com/settings/organization/api-keys", "User")
            throw new InvalidOperationException("API key is required.");
        }

        services.AddSingleton<FamilyService>();

        // Register ChatClient using Microsoft.Extensions.AI.OpenAI
        services.AddSingleton<IChatClient>(provider =>
            new OpenAI.Chat.ChatClient(llmModel, apiKey).AsIChatClient()
        );
    }
);

// Build the host
var host = builder.Build();

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

// Get logger
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Get ChatClient from DI
var openAiClient = host.Services.GetRequiredService<IChatClient>();
var chatClient = new ChatClientBuilder(openAiClient).UseFunctionInvocation().Build();
var familyService = host.Services.GetRequiredService<FamilyService>();

// Define local function wrappers for the family tools
Task<string> GetFamilyAsync() => FamilyTools.GetFamily(familyService);
Task<string?> GetPersonAsync(string id) => FamilyTools.GetPerson(familyService, id);
Task<string> AddPersonAsync(string personJson) => FamilyTools.AddPerson(familyService, personJson);
Task<string> UpdatePersonAsync(string id, string personJson) => FamilyTools.UpdatePerson(familyService, id, personJson);
Task<string> DeletePersonAsync(string id) => FamilyTools.DeletePerson(familyService, id);
Task<string> AddSpouseAsync(string id, string spouseId) => FamilyTools.AddSpouse(familyService, id, spouseId);
Task<string> AddChildAsync(string id, string childId) => FamilyTools.AddChild(familyService, id, childId);


// Create chat options with local wrappers of tools
var chatOptions = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(GetFamilyAsync),
        AIFunctionFactory.Create((string id) => GetPersonAsync(id)),
        AIFunctionFactory.Create((string person) => AddPersonAsync(person)),
        AIFunctionFactory.Create((string id, string personJson) => UpdatePersonAsync(id, personJson)),
        AIFunctionFactory.Create((string id) => DeletePersonAsync(id)),
        AIFunctionFactory.Create((string id, string spouseId) => AddSpouseAsync(id, spouseId)),
        AIFunctionFactory.Create((string id, string childId) => AddChildAsync(id, childId)),
	],
};

// Initialize conversation history
var conversation = new List<ChatMessage>();

// Add the pre-prompt instructions to the conversation
conversation.Add(new ChatMessage(ChatRole.System, string.Join(" ", Prompt.PrePromptInstructions)));

// Start the chat loop
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    // Read user input
    var userInput = Console.ReadLine();

    // the assistant has only 60 seconds timeout to generate a response
    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(60));

    // if the user input is empty, encourage them to ask a question about the family data
    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Assistant is thinking...");
        Console.ResetColor();

        var encouragmentPrompt =
            "Say something funny to encourage the user to ask a question about the family data. "
            + "Remember, you only know about names, relationships, gender and year of birth. "
            + "Don't talk about anything else.";
        var encouragingMessage = new ChatMessage(ChatRole.System, encouragmentPrompt);
        var encouragingResponse = new StringBuilder();

        await foreach (
            var messageUpdate in chatClient.GetStreamingResponseAsync(
                encouragingMessage,
                null,
                cts.Token
            )
        )
        {
            if (messageUpdate.Role == ChatRole.Assistant)
            {
                encouragingResponse.Append(messageUpdate.Text);
            }
        }

        if (encouragingResponse.Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nAssistant: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(encouragingResponse.ToString().TrimEnd());
            Console.ResetColor();
            Console.WriteLine();
        }

        continue;
    }

    if (
        string.Equals("exit", userInput, StringComparison.CurrentCultureIgnoreCase)
        || string.Equals("quit", userInput, StringComparison.CurrentCultureIgnoreCase)
    )
    {
        // Exit the chat loop
        logger.LogInformation("Exiting chat loop.");
        break;
    }

    try
    {
        conversation.Add(new ChatMessage(ChatRole.User, userInput));

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Assistant is thinking...");
        Console.ResetColor();

        var assistantResponse = new StringBuilder();

        await foreach (
            var messageUpdate in chatClient.GetStreamingResponseAsync(
                conversation,
                chatOptions,
                cts.Token
            )
        )
        {
            if (messageUpdate.Role == ChatRole.Assistant)
            {
                assistantResponse.Append(messageUpdate.Text);
            }
        }

        if (assistantResponse.Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\nAssistant: ");
            Console.ForegroundColor = ConsoleColor.White;

            var assistantMessage = assistantResponse.ToString().TrimEnd();
            conversation.Add(new ChatMessage(ChatRole.Assistant, assistantMessage));
            Console.Write(assistantMessage);
        }

        Console.ResetColor();
        Console.WriteLine("\n");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Sorry, I encountered an error: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}

host.Dispose(); // Dispose the host to clean up resources
Console.WriteLine("Goodbye!");
