using System.ComponentModel;
using FamilyTreeApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<FamilyService>();

// Log all discovered MCP tools for diagnostics
var toolTypes = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), true).Any());

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
await builder.Build().RunAsync();
