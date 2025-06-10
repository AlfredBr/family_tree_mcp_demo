namespace mcp_client_web;

public class McpSseClient(HttpClient httpClient)
{
	public Uri Endpoint => httpClient.BaseAddress ?? throw new InvalidOperationException("HTTP client base address is not set.");
}