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
builder.Logging.AddConsole(options =>
{
	// Configure all logs to go to stderr
	options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register controllers and Swagger services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.MapMcp();

await app.RunAsync();
