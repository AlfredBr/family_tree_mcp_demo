using FamilyTreeApp;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSimpleConsoleLogging();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly(typeof(FamilyTools).Assembly);

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
	options.SwaggerEndpoint("/openapi/v1.json", "SwaggerUI");
	options.EnableTryItOutByDefault();
});
app.UseHttpsRedirection();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting MCP SSE Server...");

app.MapDefaultEndpoints();
app.MapMcp();

await app.RunAsync();
