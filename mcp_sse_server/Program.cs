using mcp_sse_server;

using Microsoft.Extensions.Logging.Console;

using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
	options.TimestampFormat = "HH:mm:ss ";
	options.ColorBehavior = LoggerColorBehavior.Enabled; // Corrected namespace usage
	options.SingleLine = true;
});
builder.Logging.AddConsole(options =>
{
	// Configure all logs to go to stderr
	options.LogToStandardErrorThreshold = LogLevel.Trace;
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
    var bridge = app.Services.GetRequiredService<Bridge>();
    bridge.Writer.TryWrite(message);
})
.WithName("GetHello");

app.MapDefaultEndpoints();

await app.RunAsync();
