#pragma warning disable S1481

var builder = DistributedApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "Aspire AppHost";
var rawWebApi = builder.AddProject<Projects.family_webapi>("raw-webapi");
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
await builder.Build().RunAsync();
