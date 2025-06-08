var builder = DistributedApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "Aspire AppHost";
var webApi = builder.AddProject<Projects.family_webapi>("webapi");
//var mcpWebApi = builder.AddProject<Projects.family_webapi>("mcpwebapi")
//	.WithReference(webApi)
//	.WaitFor(webApi)
//	.WithExternalHttpEndpoints();
var mcpSseServer = builder.AddProject<Projects.mcp_sse_server>("server")
	.WithReference(webApi)
	.WaitFor(webApi)
	.WithExternalHttpEndpoints();
var frontend = builder.AddProject<Projects.blazor_frontend>("frontend")
	.WithReference(mcpSseServer)
	.WaitFor(mcpSseServer)
	.WithExternalHttpEndpoints();
await builder.Build().RunAsync();
