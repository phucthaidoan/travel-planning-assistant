using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace TravelAssistant.Tests.Resilience;

/// <summary>
/// Verifies the shared Polly resilience pipeline (US-03 AC-5):
///   - successful call returns 200
///   - transient 503 is retried until success
///   - repeated 503s trip the circuit-breaker without an unhandled exception
/// </summary>
public sealed class ResiliencePipelineTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly IHttpClientFactory _factory;

    public ResiliencePipelineTests()
    {
        _server = WireMockServer.Start();

        var services = new ServiceCollection();
        // Mirror the exact policy shape registered in Program.cs, with fast delays for tests
        services.AddHttpClient("test");
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromMilliseconds(20);  // fast for CI

                // AttemptTimeout must be set low so SamplingDuration can satisfy the
                // validation rule: SamplingDuration >= 2 * AttemptTimeout
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(5);   // > 2*2s ✓
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
            });
        });

        _factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    // AC-5a: successful call completes without retry
    [Fact]
    public async Task SuccessfulCall_ReturnsOk()
    {
        _server
            .Given(Request.Create().WithPath("/ok").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("ok"));

        var client = _factory.CreateClient("test");
        var response = await client.GetAsync($"{_server.Url}/ok");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // AC-5b: two 503s followed by 200 — retry succeeds on 3rd attempt
    [Fact]
    public async Task TransientFailure_RetriesThenSucceeds()
    {
        // WireMock scenario: start → fail1 → fail2 → succeed
        const string Scenario = "flaky";
        const string Fail1 = "fail1";
        const string Fail2 = "fail2";
        const string Succeed = "succeed";

        _server
            .Given(Request.Create().WithPath("/flaky").UsingGet())
            .InScenario(Scenario)                          // no WhenStateIs = matches initial state
            .WillSetStateTo(Fail1)
            .RespondWith(Response.Create().WithStatusCode(503));

        _server
            .Given(Request.Create().WithPath("/flaky").UsingGet())
            .InScenario(Scenario).WhenStateIs(Fail1)
            .WillSetStateTo(Fail2)
            .RespondWith(Response.Create().WithStatusCode(503));

        _server
            .Given(Request.Create().WithPath("/flaky").UsingGet())
            .InScenario(Scenario).WhenStateIs(Fail2)
            .WillSetStateTo(Succeed)
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("ok"));

        var client = _factory.CreateClient("test");
        var response = await client.GetAsync($"{_server.Url}/flaky");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        // WireMock logged 3 requests (2 retried + 1 success)
        _server.LogEntries.Count(e => "/flaky".Equals(e.RequestMessage?.Path)).Should().Be(3,
            "Polly should have retried 2 times before succeeding on the 3rd attempt");
    }

    // AC-5c: circuit-breaker opens after threshold — no unhandled exception (AC-2)
    [Fact]
    public async Task CircuitBreaker_OpensAfterThreshold_NoUnhandledException()
    {
        _server
            .Given(Request.Create().WithPath("/down").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503));

        var client = _factory.CreateClient("test");
        var exceptions = new List<Exception>();

        // Send enough requests to exhaust retries and saturate the CB minimum throughput
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await client.GetAsync($"{_server.Url}/down");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        // All failures caught — no unhandled crash (AC-2)
        exceptions.Should().NotBeEmpty("retries exhausted and/or circuit breaker opened");
    }

    public void Dispose() => _server.Stop();
}
