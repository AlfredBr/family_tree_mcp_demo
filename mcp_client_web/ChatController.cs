using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

using ModelContextProtocol.Client;

using System.Text;

using Throw;

namespace mcp_client_web;

#pragma warning disable S6931

[ApiController]
public class ChatController : ControllerBase
{
	private readonly ILogger<ChatController> _logger;
	private readonly IChatClient _chatClient;
	private readonly McpSseClient _mcpSseClient;
	private readonly ChatHistoryService _chatHistory;

	public ChatController(ILogger<ChatController> logger, IChatClient chatClient, McpSseClient mcpSseClient, ChatHistoryService chatHistoryService)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
		_mcpSseClient = mcpSseClient ?? throw new ArgumentNullException(nameof(mcpSseClient));
		_chatHistory = chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));

		_logger.LogInformation("ChatController initialized with IChatClient: {ChatClientType}", _chatClient.GetType().Name);
	}

	[HttpPost("/chat")]
	public async Task<string> Chat([FromBody] string message)
	{
		// Check if the current message is empty
		if (string.IsNullOrWhiteSpace(message))
		{
			_logger.LogWarning("Received empty message, returning empty response.");
			return string.Empty;
		}

		_logger.LogInformation("Received Chat message: '{Message}'", message);

		// Create MCP client connecting to our MCP server
		var mcpClient = await _mcpSseClient.CreateAsync();
		mcpClient.ThrowIfNull();
		_logger.LogInformation("MCP client created: {McpClientType}", mcpClient.GetType().Name);

		// Get available tools from the MCP server
		var tools = await mcpClient.ListToolsAsync();
		tools.ThrowIfNull().IfEmpty();

		// Load up the chat message history from the cache
		var messageHistory = _chatHistory.Load();

		// Set up a brand new chat
		var messages = new List<ChatMessage>();

		// If the message history is empty, add pre-prompt instructions
		if (messageHistory.Count == 0)
		{
			// Get the pre-prompt instructions
			var prePromptInstructions = string.Join(" ", FamilyTreeApp.Prompt.PrePromptInstructions);
			messages.Add(new ChatMessage(ChatRole.System, prePromptInstructions));
		}

		// Add the user's message to the chat
		messages.Add(new ChatMessage(ChatRole.User, message));

		// Get streaming response and collect updates
		List<ChatResponseUpdate> updates = [];
		var result = new StringBuilder();

		// Send the chat request (along with a set of tools) to the chat client
		await foreach (var update in _chatClient.GetStreamingResponseAsync(
			messageHistory.Concat(messages),
			new() { Tools = [.. tools] }
		))
		{
			result.Append(update);
			updates.Add(update);
		}

		// Add the assistant's responses to the message history
		messages.AddMessages(updates);

		// Log the final response
		var response = result.ToString();

		// Add the new messages to the chat history and save to cache
		_chatHistory.Save(messages);

		_logger.LogInformation("Returning Chat response: {Response}", response);
		return response;
	}
}
