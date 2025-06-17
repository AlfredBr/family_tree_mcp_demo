using System.Net;
using Microsoft.Extensions.ServiceDiscovery;
using ModelContextProtocol.Client;
using Throw;

namespace mcp_client_web;

public class McpSseClient(
    HttpClient httpClient,
    ServiceEndpointResolver endpointResolver,
    ILogger<McpSseClient> logger
)
{
    public async Task<IMcpClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        httpClient.BaseAddress.ThrowIfNull();
        logger.LogInformation(
            "Creating MCP client with base address: {BaseAddress}",
            httpClient.BaseAddress
        );

        var source = await endpointResolver.GetEndpointsAsync(
            httpClient.BaseAddress.ToString(),
            cancellationToken
        );
        var endpoint =
            source
                .Endpoints.Select(e => ConvertToUri(e.EndPoint))
                .FirstOrDefault(uri => uri is not null) ?? httpClient.BaseAddress!;

        logger.LogInformation("Resolved endpoint: {Endpoint}", endpoint);

        return await McpClientFactory.CreateAsync(
            clientTransport: new SseClientTransport(
                new SseClientTransportOptions { Endpoint = endpoint }
            ),
            cancellationToken: cancellationToken
        );
    }

    private static Uri? ConvertToUri(EndPoint ep)
    {
        if (ep == null)
        {
            return null;
        }

        var type = ep.GetType();
        // Handle internal UriEndPoint via reflection
        var uriProp = type.GetProperty("Uri");
        if (uriProp?.GetValue(ep) is Uri uri)
        {
            return uri;
        }

        return ep switch
        {
            DnsEndPoint dns => new UriBuilder("https", dns.Host, dns.Port).Uri,
            IPEndPoint ip => new UriBuilder("https", ip.Address.ToString(), ip.Port).Uri,
            _ => null,
        };
    }
}
