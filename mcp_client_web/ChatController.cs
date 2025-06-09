using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

using ModelContextProtocol.Client;

using System.Text;

namespace McpClient.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
	private readonly ILogger<ChatController> _logger;
	private readonly IChatClient _chatClient;

	public ChatController(ILogger<ChatController> logger, IChatClient chatClient)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

		_logger = logger;
		_chatClient = chatClient;
	}

	[HttpPost(Name = "Chat")]
	public async Task<string> Chat([FromBody] string message)
	{
		_logger.LogInformation("Received chat message: {Message}", message);

		// Create MCP client connecting to our MCP server
		var mcpClient = await McpClientFactory.CreateAsync(
			new SseClientTransport(
				new SseClientTransportOptions
				{
					//Endpoint = new Uri("https+http://mcp-sse-server")
					Endpoint = new Uri("http://localhost:5002/sse")
				}
			)
		);
		// Get available tools from the MCP server
		var tools = await mcpClient.ListToolsAsync();
		// Set up the chat messages
		var messages = new List<ChatMessage>
		{
			new ChatMessage(ChatRole.System, "You are a helpful assistant.")
		};
		messages.Add(new(ChatRole.User, message));
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
		return result.ToString();
	}
}