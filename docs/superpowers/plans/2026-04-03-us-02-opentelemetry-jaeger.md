# US-02 — OpenTelemetry + Jaeger Observability Baseline

**Date:** 2026-04-03
**Story:** US-02
**Status:** In Progress

---

## Context

US-01 delivered the solution scaffold with MAF RC4, docker-compose (Jaeger on 4317/16686), and `JaegerOptions` config. US-02 adds the OTel instrumentation layer so every agent invocation and outbound HTTP call produces a trace in Jaeger from day one.

Because US-04 hasn't wired any real `IChatClient` yet, this story:
- Installs OTel packages and registers the pipeline via `AddOpenTelemetry()`
- Provides a static `TravelActivitySource` that all future agents will use
- Stamps `agent.session_id` on spans via the updated placeholder handler
- Adds `AddHttpClientInstrumentation` so outbound HTTP child spans are automatic
- Verifies the pipeline with an in-memory exporter xUnit test (no Jaeger required in CI)

---

## Acceptance Criteria

| # | Criterion | Approach |
|---|-----------|----------|
| 1 | `AddOpenTelemetry()` with GenAI conventions; spans in Jaeger | Hosted `AddOpenTelemetry()` + OTLP exporter to `JaegerOptions.Endpoint` |
| 2 | Each agent span includes `AgentSession ID` attribute | `TravelActivitySource.StartAgentSession(sessionId)` sets `agent.session_id` tag |
| 3 | Outbound HTTP child spans | `AddHttpClientInstrumentation()` on tracer builder |
| 4 | Smoke test: ≥1 complete trace | xUnit with `AddInMemoryExporter` |
| 5 | Span count > 0 in integration test | Same test; `exportedActivities.Count.Should().BeGreaterThan(0)` |

---

## NuGet Packages

| Project | Package | Version |
|---------|---------|---------|
| Console | `OpenTelemetry.Extensions.Hosting` | 1.15.1 |
| Console | `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.15.1 |
| Console | `OpenTelemetry.Instrumentation.Http` | 1.15.0 |
| Core | `OpenTelemetry.Api` | 1.15.1 |
| Tests | `OpenTelemetry` | 1.15.1 |
| Tests | `OpenTelemetry.Exporter.InMemory` | 1.15.1 |

---

## Files Changed

| File | Change |
|------|--------|
| `src/TravelAssistant.Core/Telemetry/TravelActivitySource.cs` | Created — static `ActivitySource` + `StartAgentSession` |
| `src/TravelAssistant.Core/TravelAssistant.Core.csproj` | Added `OpenTelemetry.Api` |
| `src/TravelAssistant.Console/Program.cs` | Added `AddOpenTelemetry()` registration + span in placeholder handler |
| `src/TravelAssistant.Console/TravelAssistant.Console.csproj` | Added 3 OTel packages |
| `tests/TravelAssistant.Tests/Telemetry/TelemetryPipelineTests.cs` | Created — in-memory exporter xUnit test |
| `tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj` | Added 2 OTel test packages |

---

## Tasks

- [x] Task 1: Add NuGet packages to all three .csproj files
- [x] Task 2: Create `TravelActivitySource` in Core
- [x] Task 3: Register `AddOpenTelemetry()` in Program.cs
- [x] Task 4: Emit span from placeholder handler
- [x] Task 5: Write `TelemetryPipelineTests`
- [x] Task 6: Write this plan file
- [x] Task 7: Update sprint-board, backlog-board, release-board

---

## Verification

```bash
dotnet build
dotnet test
# Manual: docker compose up -d
# dotnet run --project src/TravelAssistant.Console
# Open http://localhost:16686 → search service "TravelAssistant"
# Confirm AgentSession span with agent.session_id tag
```
