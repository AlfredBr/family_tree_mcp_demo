using System.Text.Json;
using Microsoft.Extensions.Logging.Console;

#pragma warning disable S125

// Create a new web application
var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    //options.IncludeScopes = true;
    //options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Enabled; // Corrected namespace usage
    options.SingleLine = true;
});

var app = builder.Build();

// Get logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Read the people.json file once at startup
string formattedJson;
try
{
    var json = await File.ReadAllTextAsync("people.json");
    var doc = JsonDocument.Parse(json);
    var options = new JsonSerializerOptions { WriteIndented = true };
    formattedJson = JsonSerializer.Serialize(doc.RootElement, options);
    logger.LogInformation("Successfully loaded people.json file at startup");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error loading people.json file");
    formattedJson = "[]"; // Provide empty array as fallback
}

app.MapGet(
    "/people",
    () =>
    {
        logger.LogInformation("/people endpoint accessed at {Time}", DateTime.Now);
        return Results.Text(formattedJson, "application/json");
    }
);

await app.RunAsync();
