using basic_sse_server;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
	options.TimestampFormat = "HH:mm:ss ";
	options.ColorBehavior = LoggerColorBehavior.Enabled;
	options.SingleLine = true;
});
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

app.MapPost("/hello", ([FromBody] string message) =>
{
	var logger = app.Services.GetRequiredService<ILogger<Program>>();
	var bridge = app.Services.GetRequiredService<Bridge>();
	bridge.Writer.TryWrite(message);
	logger.LogInformation("Message sent: {Message}", message);
	return $"/hello '{message}'";
})
.WithName("SayHello");

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
