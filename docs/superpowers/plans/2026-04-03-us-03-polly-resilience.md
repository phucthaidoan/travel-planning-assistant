# US-03 — Polly Resilience on All Outbound Calls

**Date:** 2026-04-03
**Story:** US-03
**Status:** Done

---

## Context

US-01 (scaffold) and US-02 (OTel) done. US-03 wraps every outbound `HttpClient` with Polly retry + circuit-breaker so transient failures don't crash the assistant. Uses `Microsoft.Extensions.Http.Resilience` (the modern .NET 10 wrapper over Polly 8).

Real OpenAI/Qdrant typed clients are deferred to US-04+; this story registers named placeholder clients that inherit the shared policy automatically.

---

## Acceptance Criteria

| # | Criterion | Approach |
|---|-----------|----------|
| 1 | Shared resilience: exponential retry (3 attempts), CB (5 failures / 30s break) | `ConfigureHttpClientDefaults` + `AddStandardResilienceHandler()` |
| 2 | 503 triggers retry + CB open, no unhandled exception | WireMock integration test |
| 3 | All `IHttpClientFactory` clients inherit shared policy | `ConfigureHttpClientDefaults` applies globally |
| 4 | Polly events emit OTel span events | `AddResilienceEnricher()` on `IServiceCollection` |
| 5 | Test covers: success, retry-then-success, CB open | 3 xUnit tests — all passing |

---

## NuGet Packages Added

| Project | Package | Version |
|---------|---------|---------|
| Console | `Microsoft.Extensions.Http.Resilience` | 10.4.0 |
| Tests | `Microsoft.Extensions.Http.Resilience` | 10.4.0 |
| Tests | `WireMock.Net` | 2.2.0 |

---

## Files Changed

| File | Change |
|------|--------|
| `src/TravelAssistant.Console/Program.cs` | Added named clients `"openai"` / `"qdrant"`, `ConfigureHttpClientDefaults` with resilience, `AddResilienceEnricher()` |
| `src/TravelAssistant.Console/TravelAssistant.Console.csproj` | Added `Microsoft.Extensions.Http.Resilience` |
| `tests/TravelAssistant.Tests/Resilience/ResiliencePipelineTests.cs` | Created — 3 xUnit tests with WireMock |
| `tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj` | Added resilience + WireMock packages, bumped `Configuration.Binder` to 10.0.4 |

---

## Tasks

- [x] Task 1: Add NuGet packages
- [x] Task 2: Register resilience pipeline in Program.cs
- [x] Task 3: Write xUnit resilience tests
- [x] Task 4: Write this plan file
- [x] Task 5: Update sprint-board, backlog-board, release-board

---

## Verification

```bash
dotnet build   # 0 errors, 0 warnings
dotnet test    # 6/6 passed
```
