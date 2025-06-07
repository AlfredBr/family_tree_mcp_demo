var builder = DistributedApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "Aspire AppHost";
var webapi = builder.AddProject<Projects.family_webapi>("webapi");
var mcpSseServer = builder.AddProject<Projects.mcp_sse_server>("server")
	.WithReference(webapi)
	.WaitFor(webapi)
	.WithExternalHttpEndpoints();
var frontend = builder.AddProject<Projects.blazor_frontend>("frontend")
	.WithReference(mcpSseServer)
	.WaitFor(mcpSseServer)
	.WithExternalHttpEndpoints();
await builder.Build().RunAsync();
