using ModelContextProtocol.Client;

using Throw;

namespace mcp_client_web;

public class McpSseClient(HttpClient httpClient, ILogger<McpSseClient> logger)
{
	public async Task<IMcpClient> CreateAsync(CancellationToken cancellationToken = default)
	{
		httpClient.BaseAddress.ThrowIfNull();
		logger.LogInformation("Creating MCP client with base address: {BaseAddress}", httpClient.BaseAddress);
		return await McpClientFactory.CreateAsync(
			clientTransport: new SseClientTransport(
				new SseClientTransportOptions
				{
					Endpoint = new Uri("https://localhost:7040") // httpClient.BaseAddress
				}
			),
			cancellationToken: cancellationToken
		);
	}
}