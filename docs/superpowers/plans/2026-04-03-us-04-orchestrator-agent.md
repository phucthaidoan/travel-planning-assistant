# US-04 — OrchestratorAgent with Session Lifecycle

**Date:** 2026-04-03
**Story:** US-04
**Epic:** EP-02 (Orchestration & Workflow)
**Sprint:** Sprint 1

---

## Context

US-01–03 are done: scaffold, OTel, and Polly are all wired. `Program.cs` calls a `PlaceholderHandler` that echoes input. US-04 replaces that placeholder with a real MAF RC4 `OrchestratorAgent` that accepts user messages, routes them to specialist stubs via LLM tool-calling, preserves conversation history across turns via `AgentSession`, and emits OTel spans with `agent.session_id` and `routing_target` tags.

**Key discovery:** `AgentWorkflowBuilder` does not exist in MAF RC4. The correct RC4 pattern: register specialist agents as `AIFunction` tools via `stub.AsAIFunction()`, add middleware via `.AsBuilder().Use(Middleware).Build()` to intercept tool calls, and drive the loop from host code.

---

## New Files

### `src/TravelAssistant.Core/Agents/SpecialistStub.cs`

Static factory — creates a named `ChatClientAgent` stub for a specialist domain.

```csharp
namespace TravelAssistant.Core.Agents;

public static class SpecialistStub
{
    public static ChatClientAgent Create(IChatClient chatClient, string name, string instructions)
        => chatClient.AsAIAgent(name: name, instructions: instructions);
}
```

### `src/TravelAssistant.Core/Agents/OrchestratorAgentFactory.cs`

Builds the orchestrator with specialist tools and tool-call middleware for OTel routing tag.

```csharp
public static AIAgent Create(
    IChatClient chatClient,
    IEnumerable<ChatClientAgent> specialists,
    Action<string>? onToolInvoked = null)
```

- Registers each specialist as `stub.AsAIFunction()`
- Adds middleware via `.AsBuilder().Use(...)` — calls `onToolInvoked(ctx.Function.Name)` on every tool call
- One specialist for US-04: `itinerary_agent`

### `tests/TravelAssistant.Tests/Agents/OrchestratorSessionTests.cs`

- **AC-3:** Two RunAsync calls on same session → WireMock asserts second request body includes prior history
- **AC-5:** WireMock returns tool-call response → assert `onToolInvoked` fires with `"itinerary_agent"` (and/or OTel span has `routing_target` tag)

---

## Modified Files

### `src/TravelAssistant.Console/Program.cs`

Replace lines 64–73 (PlaceholderHandler + ConsoleLoop.RunAsync(PlaceholderHandler)) with:

1. Resolve `AssistantOptions` + `IHttpClientFactory` from `host.Services`
2. Build `OpenAIClient` with `HttpClientPipelineTransport(httpClient)` using "openai" named client
3. Start session span via `TravelActivitySource.StartAgentSession(sessionId)`
4. Build `itineraryStub` and `orchestrator` via factories
5. `await orchestrator.CreateSessionAsync()` — get `AgentSession`
6. `ConsoleLoop.RunAsync(msg => orchestrator.RunAsync(msg, session))`

### `tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj`

Add: `<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-rc4" />`

---

## OTel Tag Strategy

| Tag | Value | Where |
|-----|-------|-------|
| `agent.session_id` | `Guid.NewGuid().ToString()` | `TravelActivitySource.StartAgentSession()` — existing |
| `routing_target` | `ctx.Function.Name` | Middleware callback → `sessionSpan?.SetTag(...)` |

The `sessionSpan` is captured in Program.cs before the loop and passed into `onToolInvoked` via closure — avoids `Activity.Current` ambiguity inside MAF internals.

---

## Verification

1. `dotnet build` — clean
2. `dotnet test` — all pass
3. `dotnet run --project src/TravelAssistant.Console` — type "Plan a trip to Paris", get itinerary agent response
4. Jaeger UI `http://localhost:16686` — `AgentSession` span has both tags after a routing turn
