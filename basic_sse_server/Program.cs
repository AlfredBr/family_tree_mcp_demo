using sse_server;

using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
	options.TimestampFormat = "HH:mm:ss ";
	options.ColorBehavior = LoggerColorBehavior.Enabled;
	options.SingleLine = true;
});

// Register controllers and Swagger services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<Bridge>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.ConfigObject.AdditionalItems["tryItOutEnabled"] = true;
	});
	app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/hello/{message}", (string message) =>
{
	var logger = app.Services.GetRequiredService<ILogger<Program>>();
	var bridge = app.Services.GetRequiredService<Bridge>();
	bridge.Writer.TryWrite(message);
	logger.LogInformation("Message sent: {Message}", message);
	return $"Hello {message}";
})
.WithName("GetHello");

app.MapDefaultEndpoints();

await app.RunAsync();
