using System.ComponentModel;

using FamilyTreeApp.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
builder.Services.AddSingleton<FamilyService>();
await builder.Build().RunAsync();
