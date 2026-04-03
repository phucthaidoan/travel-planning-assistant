using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAssistant.Console;
using TravelAssistant.Core.Configuration;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services.Configure<AssistantOptions>(builder.Configuration);
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});

IHost host = builder.Build();

// Placeholder handler — replaced in US-04 when OrchestratorAgent is wired
static Task<string> PlaceholderHandler(string message) =>
    Task.FromResult($"[Scaffold] Echo: {message} (agent not yet wired — US-04)");

await ConsoleLoop.RunAsync(PlaceholderHandler);
