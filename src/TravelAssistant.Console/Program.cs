using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ClientModel;
using System.ClientModel.Primitives;
using TravelAssistant.Console;
using TravelAssistant.Core.Agents;
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
await host.StartAsync();

// Read OpenAI credentials directly from configuration (user secrets override appsettings.json)
var apiKey  = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException(
        "OpenAI:ApiKey is not configured. Set it via user secrets: " +
        "dotnet user-secrets set \"OpenAI:ApiKey\" \"<key>\" --project src/TravelAssistant.Console");
var modelId = builder.Configuration["OpenAI:ChatModelId"] ?? "gpt-4.1-nano";

var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
var httpClient = httpClientFactory.CreateClient("openai");

// Build OpenAI client using the named HttpClient (Polly resilience pipeline applied automatically)
var openAiClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) });
IChatClient chatClient = openAiClient
    .GetChatClient(modelId)
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(
        sourceName: TravelActivitySource.ServiceName,
        configure: c => c.EnableSensitiveData = true)
    .Build();

// Session span — lives for the duration of the console loop (AC-5: agent.session_id tag)
string sessionId = Guid.NewGuid().ToString();
using var sessionSpan = TravelActivitySource.StartAgentSession(sessionId);

// Build OrchestratorAgent with one routing edge (AC-1: itinerary_agent)
ChatClientAgent itineraryStub = SpecialistStub.Create(
    chatClient,
    "itinerary_agent",
    "You are a travel itinerary planning specialist. Help users plan trip itineraries.");

AIAgent orchestrator = OrchestratorAgentFactory.Create(
    chatClient,
    [itineraryStub],
    onToolInvoked: toolName => sessionSpan?.SetTag("routing_target", toolName));

// AC-2: CreateSessionAsync produces a session; RunAsync executes the pipeline
AgentSession session = await orchestrator.CreateSessionAsync();

// AC-4: Console loop continuously reads input and calls RunAsync (AC-3: same session preserves history)
await ConsoleLoop.RunAsync(async message =>
{
    AgentResponse response = await orchestrator.RunAsync(message, session);
    return response.Text;
});

await host.StopAsync();
