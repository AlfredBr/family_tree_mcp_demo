#pragma warning disable S1481

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

var builder = DistributedApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "Aspire AppHost";
builder.Services.AddLogging(loggingBuilder =>
{
	loggingBuilder.ClearProviders();
	loggingBuilder.AddSimpleConsole(options =>
	{
		options.TimestampFormat = "HH:mm:ss ";
		options.ColorBehavior = LoggerColorBehavior.Enabled;
		options.SingleLine = true;
	});
});

var rawWebApi = builder.AddProject<Projects.family_webapi>("raw-webapi");
var basicSseServer = builder.AddProject<Projects.basic_sse_server>("basic-sse-server");
var mcpWebApi = builder.AddProject<Projects.mcp_webapi>("mcp-webapi")
	.WithReference(rawWebApi)
	.WaitFor(rawWebApi)
	.WithExternalHttpEndpoints();
var mcpSseServer = builder.AddProject<Projects.mcp_sse_server>("mcp-sse-server")
	.WithReference(rawWebApi)
	.WaitFor(rawWebApi)
	.WithExternalHttpEndpoints();
var blazorFrontend = builder.AddProject<Projects.blazor_frontend>("blazor-frontend")
	.WithReference(mcpSseServer)
	.WaitFor(mcpSseServer)
	.WithExternalHttpEndpoints();
var mcpClientWeb = builder.AddProject<Projects.mcp_client_web>("mcp-client-web")
	.WithReference(mcpSseServer)
	.WaitFor(mcpSseServer)
	.WithExternalHttpEndpoints();
await builder.Build().RunAsync();
