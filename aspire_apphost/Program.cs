var builder = DistributedApplication.CreateBuilder(args);
builder.Environment.ApplicationName = "Aspire AppHost";
var webapi = builder.AddProject<Projects.family_webapi>("webapi");
var frontend = builder.AddProject<Projects.blazor_frontend>("frontend")
	.WithReference(webapi)
	.WaitFor(webapi)
	.WithExternalHttpEndpoints();
await builder.Build().RunAsync();
