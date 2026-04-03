# CLAUDE.md — Intelligent Travel Planning Assistant

> Read this file first. It orients you for every session.

---

## What this project is

A .NET 10 console application that uses Microsoft Agent Framework (MAF) RC4 to coordinate
seven specialist AI agents (Currency, Weather, Flight, Hotel, Culture, Itinerary, Orchestrator)
across a 3-tier memory stack: Qdrant RAG (L1), `ProviderSessionState<TripPlan>` (L2),
PostgreSQL JSONB (L3).

Infrastructure: PostgreSQL (raw Npgsql), Qdrant, OpenTelemetry + Jaeger, Polly.
All services run locally via Docker Compose.

---

## Hard Constraints

- **NEVER run git commands.** The user manages all version control manually.
- **One story at a time.** Always confirm with the user before starting the next story.
- **Write a plan first.** Before writing any code, create an implementation plan at
  `docs/superpowers/plans/YYYY-MM-DD-<story-id>-<slug>.md`.
- **Update boards on every status change.** When a story moves to In Progress or Done,
  update `sprint-board.md`, `backlog-board.md`, and `release-board.md`.
- **Target .NET 10 only.** `global.json` pins SDK 10.0.201. Never reference net8.0 or net9.0.
- **No EF Core.** Persistence uses raw Npgsql ADO.NET exclusively.
- **MAF package pin:** `Microsoft.Agents.AI.OpenAI` v1.0.0-rc4. Do not upgrade.

---

## Current State

> Always re-read `docs/boards/sprint-board.md` to confirm current status — this table may be stale.

| Sprint | Story | Status |
|--------|-------|--------|
| Sprint 1 | US-01 Solution scaffold and MAF RC4 wiring | Done |
| Sprint 1 | US-02 OpenTelemetry + Jaeger observability baseline | Next |
| Sprint 1 | US-03 Polly resilience on all outbound calls | To Do |
| Sprint 1 | US-04 OrchestratorAgent with session lifecycle | To Do |
| Sprint 1 | US-06 Message history reduction pipeline | To Do |

---

## Key File Locations

| What | Where |
|------|-------|
| Active sprint status | `docs/boards/sprint-board.md` |
| All stories + sprint assignments | `docs/boards/backlog-board.md` |
| MVP release gate checklist | `docs/boards/release-board.md` |
| Epic status | `docs/boards/epic-board.md` |
| Full backlog + architecture decisions | `docs/superpowers/specs/2026-04-01-agile-delivery-plan-design.md` |
| Per-story implementation plans | `docs/superpowers/plans/` |
| Console entry point | `src/TravelAssistant.Console/` |
| Shared library | `src/TravelAssistant.Core/` |
| Test project | `tests/TravelAssistant.Tests/` |
| Docker services | `docker-compose.yml` |

---

## Reference Codebases (read-only — do not modify)

| What | Path |
|------|------|
| MAF RC4 working examples (V1–V8 per topic) | `C:\Workspaces\Projects\pet\ms-agent-framework-playground\samples\` |
| Official MAF YAML agent declarations | `C:\Workspaces\git\microsoft\agent-framework\agent-samples\` |

Before writing any MAF-specific code, check these for the correct RC4 API surface.

---

## How to Start Working

1. Read `docs/boards/sprint-board.md` — confirm the next story and its status.
2. Read the story's acceptance criteria in `docs/superpowers/specs/2026-04-01-agile-delivery-plan-design.md`.
3. Check `docs/superpowers/plans/` — if no plan file exists for the story, create one before writing any code.
4. Check the MAF RC4 reference codebase for patterns the story touches.
5. Ask the user to confirm scope before starting implementation.
6. Mark the story `In Progress` in all three board files when work begins.
7. When done, mark story `Done` in all three board files and report to the user.
8. **Do NOT begin the next story — wait for explicit user instruction.**

---

## Solution Structure

```
TravelAssistant.slnx
global.json                        ← SDK 10.0.201 pinned
src/
  TravelAssistant.Console/         ← Entry point, ConsoleLoop, appsettings
  TravelAssistant.Core/            ← Shared: agents, state, config
tests/
  TravelAssistant.Tests/           ← xUnit + FluentAssertions
docker-compose.yml                 ← PostgreSQL 15 + Qdrant v1.9.1 + Jaeger 1.57
docs/boards/                       ← Living status boards (always update)
docs/superpowers/specs/            ← Spec and delivery plan (reference only)
docs/superpowers/plans/            ← Per-story plans (write here before coding)
```
