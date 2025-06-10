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

	public ChatController(ILogger<ChatController> logger, IChatClient chatClient, McpSseClient mcpSseClient)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
		_mcpSseClient = mcpSseClient ?? throw new ArgumentNullException(nameof(mcpSseClient));

		_logger.LogInformation("ChatController initialized with IChatClient: {ChatClientType}", _chatClient.GetType().Name);
	}

	[HttpPost("/chat")]
	public async Task<string> Chat([FromBody] string message)
	{
		_logger.LogInformation("Received Chat message: '{Message}'", message);

		// Create MCP client connecting to our MCP server
		var mcpClient = await _mcpSseClient.CreateAsync();
		mcpClient.ThrowIfNull();
		_logger.LogInformation("MCP client created: {McpClientType}", mcpClient.GetType().Name);

		// Get available tools from the MCP server
		var tools = await mcpClient.ListToolsAsync();
		tools.ThrowIfNull().IfEmpty();

		// Get the pre-prompt instructions
		var prePromptInstructions = string.Join(" ", FamilyTreeApp.Prompt.PrePromptInstructions);

		// Set up the chat messages
		var messages = new List<ChatMessage>();
		messages.Add(new ChatMessage(ChatRole.System, prePromptInstructions));
		messages.Add(new ChatMessage(ChatRole.User, message));

		// Get streaming response and collect updates
		List<ChatResponseUpdate> updates = [];
		var result = new StringBuilder();

		await foreach (var update in _chatClient.GetStreamingResponseAsync(
			messages,
			new() { Tools = [.. tools] }
		))
		{
			result.Append(update);
			updates.Add(update);
		}

		// Add the assistant's responses to the message history
		messages.AddMessages(updates);
		var response = result.ToString();

		_logger.LogInformation("Returning Chat response: {Response}", response);
		return response;
	}
}
