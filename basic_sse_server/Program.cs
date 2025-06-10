using basic_sse_server;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSimpleConsoleLogging();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<Bridge>();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
	options.SwaggerEndpoint("/openapi/v1.json", "SwaggerUI");
	options.EnableTryItOutByDefault();
});
app.UseHttpsRedirection();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Basic SSE Server...");

app.MapPost("/hello", ([FromBody] string message) =>
{
	var bridge = app.Services.GetRequiredService<Bridge>();
	bridge.Writer.TryWrite(message);
	logger.LogInformation("Message sent: {Message}", message);
	return $"/hello '{message}'";
})
.WithName("SayHello");

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
