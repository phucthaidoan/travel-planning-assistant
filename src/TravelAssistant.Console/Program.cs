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
        .AddHttpClientInstrumentation()           // AC-3 (US-02): outbound HTTP child spans
        .AddOtlpExporter(opt =>
            opt.Endpoint = new Uri(jaegerEndpoint)));

// AC-4 (US-03): emit Polly retry/circuit-breaker events onto the active OTel span
builder.Services.AddResilienceEnricher();

// Named HttpClients for OpenAI and Qdrant — real clients wired in US-04+
builder.Services.AddHttpClient("openai");
builder.Services.AddHttpClient("qdrant");

// Shared Polly resilience pipeline applied to all IHttpClientFactory clients (AC-1, AC-3)
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        // Exponential-backoff retry — 3 attempts
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromSeconds(1);

        // Circuit-breaker — trips after 5 failures in a 30s window, breaks for 30s
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    });
});

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
