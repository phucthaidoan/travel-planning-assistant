using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TravelAssistant.Console;
using TravelAssistant.Core.Configuration;
using TravelAssistant.Core.Telemetry;

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

var jaegerEndpoint = builder.Configuration["Jaeger:Endpoint"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault().AddService(TravelActivitySource.ServiceName))
        .AddSource(TravelActivitySource.ServiceName)
        .AddHttpClientInstrumentation()           // AC-3: outbound HTTP calls become child spans
        .AddOtlpExporter(opt =>
            opt.Endpoint = new Uri(jaegerEndpoint)));

IHost host = builder.Build();

// Placeholder handler — replaced in US-04 when OrchestratorAgent is wired
static async Task<string> PlaceholderHandler(string message)
{
    var sessionId = Guid.NewGuid().ToString();
    using var span = TravelActivitySource.StartAgentSession(sessionId);
    await Task.Yield();
    return $"[Scaffold] Echo: {message} (agent not yet wired — US-04)";
}

await ConsoleLoop.RunAsync(PlaceholderHandler);
