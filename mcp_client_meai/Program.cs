using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using FamilyTreeApp;
using Microsoft.Extensions.AI;

// Build configuration
var builder = Host.CreateDefaultBuilder(args);

// Configure logging
builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.ColorBehavior = LoggerColorBehavior.Enabled;
        options.SingleLine = true;
    });
});

// Configure services
string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Write("Enter your OpenAI API key: ");
    apiKey = Console.ReadLine();
}
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("API key is required.");
}

// Start MCP server (same as your original code)
var mcpLibraryProject = Path.Combine(
    AppContext.BaseDirectory,
    "..",
    "..",
    "..",
    "..",
    "mcp_library",
    "mcp_library.csproj"
);
if (!File.Exists(mcpLibraryProject))
{
    Console.WriteLine($"MCP library project not found at {mcpLibraryProject}.");
    return;
}

var mcpProcess = new System.Diagnostics.Process
{
    StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{mcpLibraryProject}\"",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = AppContext.BaseDirectory,
    },
};

mcpProcess.Start();
var mcpInput = mcpProcess.StandardInput;
var mcpOutput = mcpProcess.StandardOutput;

// Background task for stderr
_ = Task.Run(async () =>
{
    string? line;
    while ((line = await mcpProcess.StandardError.ReadLineAsync()) != null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[MCP STDERR] {line}");
        Console.ResetColor();
    }
});

await Task.Delay(10_000); // Wait for MCP server to start

// Chat loop
while (true)
{

}

// Cleanup
mcpProcess.Kill();
