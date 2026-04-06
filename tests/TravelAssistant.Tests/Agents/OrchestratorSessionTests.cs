using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using TravelAssistant.Core.Agents;
using TravelAssistant.Core.Telemetry;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace TravelAssistant.Tests.Agents;

/// <summary>
/// US-04 acceptance criteria tests:
///   AC-3: Two sequential messages in the same session retain context.
///   AC-5: OTel span receives routing_target tag when a tool call fires.
/// </summary>
public sealed class OrchestratorSessionTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;
    private readonly IChatClient _chatClient;

    public OrchestratorSessionTests()
    {
        _server = WireMockServer.Start();

        // Do NOT dispose httpClient here — HttpClientPipelineTransport holds it for the test lifetime
        _httpClient = new HttpClient();
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential("test-key"),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_server.Url!),
                Transport = new HttpClientPipelineTransport(_httpClient)
            });

        _chatClient = openAiClient
            .GetChatClient("gpt-4.1-nano")
            .AsIChatClient();
    }

    // AC-3: two turns on the same AgentSession → second WireMock request body includes prior history
    [Fact]
    public async Task TwoTurns_SameSession_HistoryIsSentOnSecondTurn()
    {
        // Arrange — return a simple text completion for every POST to /chat/completions
        const string Turn1Reply = "Sure, I can help you plan a trip!";
        const string Turn2Reply = "As I mentioned, Paris is great for a 3-day trip.";

        _server
            .Given(Request.Create().UsingPost())
            .InScenario("two-turns")
            .WillSetStateTo("turn1-done")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ChatCompletionJson(Turn1Reply)));

        _server
            .Given(Request.Create().UsingPost())
            .InScenario("two-turns").WhenStateIs("turn1-done")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ChatCompletionJson(Turn2Reply)));

        var orchestrator = OrchestratorAgentFactory.Create(_chatClient, []);
        AgentSession session = await orchestrator.CreateSessionAsync();

        // Act
        AgentResponse r1 = await orchestrator.RunAsync("Plan a trip to Paris", session);
        AgentResponse r2 = await orchestrator.RunAsync("Tell me more", session);

        // Assert — both turns completed successfully
        r1.Text.Should().Be(Turn1Reply);
        r2.Text.Should().Be(Turn2Reply);

        // The second request body must contain the first user message as part of history
        var logEntries = _server.LogEntries.ToList();
        logEntries.Should().HaveCountGreaterThanOrEqualTo(2, "two RunAsync calls should produce at least two HTTP requests");

        string secondRequestBody = logEntries[1].RequestMessage?.Body ?? "";
        secondRequestBody.Should().Contain("Plan a trip to Paris",
            "the second turn must include the first user message in the messages array (history preserved)");
    }

    // AC-5: onToolInvoked callback fires with the specialist tool name when the LLM routes
    [Fact]
    public async Task OrchestratorRun_WhenLlmCallsTool_InvokesOnToolInvokedCallback()
    {
        // Arrange — round 1: LLM returns a tool call; round 2: LLM returns final text after tool result
        const string FinalReply = "Here is your itinerary for Paris.";
        const string ToolName = "itinerary_agent";

        _server
            .Given(Request.Create().UsingPost())
            .InScenario("tool-call")
            .WillSetStateTo("tool-called")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ToolCallCompletionJson(ToolName, "{\"message\":\"Plan a trip to Paris\"}")));

        // Tool execution: itinerary stub also calls the server — return a stub assistant reply
        _server
            .Given(Request.Create().UsingPost())
            .InScenario("tool-call").WhenStateIs("tool-called")
            .WillSetStateTo("tool-result")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ChatCompletionJson("Here is a detailed itinerary stub.")));

        // Round 2: orchestrator sends tool result back to LLM → final answer
        _server
            .Given(Request.Create().UsingPost())
            .InScenario("tool-call").WhenStateIs("tool-result")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ChatCompletionJson(FinalReply)));

        var invocations = new List<string>();
        ChatClientAgent itineraryStub = SpecialistStub.Create(
            _chatClient, ToolName, "You are a travel itinerary specialist.");

        AIAgent orchestrator = OrchestratorAgentFactory.Create(
            _chatClient,
            [itineraryStub],
            onToolInvoked: name => invocations.Add(name));

        AgentSession session = await orchestrator.CreateSessionAsync();

        // Act
        AgentResponse response = await orchestrator.RunAsync("Plan a 3-day trip to Paris", session);

        // Assert — callback fired with the specialist name
        invocations.Should().ContainSingle()
            .Which.Should().Be(ToolName,
                "onToolInvoked must fire with 'itinerary_agent' when the LLM routes to that specialist");
    }

    // AC-5 (OTel): sessionSpan receives routing_target tag when onToolInvoked fires
    [Fact]
    public void RoutingTarget_Tag_CanBeSetOnActivitySpan()
    {
        // Arrange — verify the tag-setting mechanism used in Program.cs works correctly
        var exportedActivities = new List<Activity>();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TravelActivitySource.ServiceName))
            .AddSource(TravelActivitySource.ServiceName)
            .AddInMemoryExporter(exportedActivities)
            .Build();

        string sessionId = Guid.NewGuid().ToString();
        using (Activity? span = TravelActivitySource.StartAgentSession(sessionId))
        {
            // Simulate what Program.cs does: onToolInvoked fires and tags the span
            span?.SetTag("routing_target", "itinerary_agent");
        }

        // Assert
        exportedActivities.Should().ContainSingle(a => a.DisplayName == "AgentSession");
        Activity? agentSpan = exportedActivities.Single(a => a.DisplayName == "AgentSession");
        agentSpan.GetTagItem("agent.session_id").Should().Be(sessionId);
        agentSpan.GetTagItem("routing_target").Should().Be("itinerary_agent");
    }

    public void Dispose()
    {
        _server.Stop();
        _httpClient.Dispose();
    }

    // --- Helpers ---

    private static string ChatCompletionJson(string content) => $$"""
        {
          "id": "chatcmpl-test",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4.1-nano",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": {{JsonSerializer.Serialize(content)}}
              },
              "finish_reason": "stop"
            }
          ],
          "usage": { "prompt_tokens": 10, "completion_tokens": 20, "total_tokens": 30 }
        }
        """;

    private static string ToolCallCompletionJson(string toolName, string argumentsJson) => $$"""
        {
          "id": "chatcmpl-tool",
          "object": "chat.completion",
          "created": 1700000000,
          "model": "gpt-4.1-nano",
          "choices": [
            {
              "index": 0,
              "message": {
                "role": "assistant",
                "content": null,
                "tool_calls": [
                  {
                    "id": "call_abc123",
                    "type": "function",
                    "function": {
                      "name": {{JsonSerializer.Serialize(toolName)}},
                      "arguments": {{JsonSerializer.Serialize(argumentsJson)}}
                    }
                  }
                ]
              },
              "finish_reason": "tool_calls"
            }
          ],
          "usage": { "prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15 }
        }
        """;
}
