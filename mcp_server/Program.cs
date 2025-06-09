using FamilyTreeApp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using ModelContextProtocol.Server;

using System.ComponentModel;
using System.Reflection;

#pragma warning disable S125

// Build configuration
var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.ColorBehavior = LoggerColorBehavior.Enabled; // Corrected namespace usage
    options.SingleLine = true;
});

// add the family service as a singleton
builder.Services.AddSingleton<FamilyServiceClient>();

// Log all discovered MCP tools for diagnostics
var toolTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), true).Any());

// Log all discovered MCP tools for diagnostics
foreach (var type in toolTypes)
{
    var toolMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), true).Any());

    foreach (var method in toolMethods)
    {
        await Console.Error.WriteLineAsync($"[MCP TOOL] Registered tool: {type.FullName}.{method.Name}");
    }
}

// Use type-based tool registration for best compatibility
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly(typeof(FamilyTools).Assembly);

// start the server
await builder.Build().RunAsync();
