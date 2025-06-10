using ModelContextProtocol.Client;

namespace mcp_client_web;

public class McpSseClient(HttpClient httpClient)
{
	public async Task<IMcpClient> CreateAsync(CancellationToken cancellationToken = default)
	{
		return await McpClientFactory.CreateAsync(
			clientTransport: new SseClientTransport(
				new SseClientTransportOptions
				{
					Endpoint = httpClient.BaseAddress ?? throw new InvalidOperationException("HTTP client base address is not set.")
				}
			),
			cancellationToken: cancellationToken
		);
	}
}