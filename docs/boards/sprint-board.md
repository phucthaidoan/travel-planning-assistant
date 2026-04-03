# Sprint Board — Intelligent Travel Planning Assistant

> Update story status as work progresses. Statuses: `To Do` | `In Progress` | `Done` | `Blocked`
> Update `Active Sprint` header when a new sprint begins.

---

## Active Sprint: Sprint 1
**Goal:** Stand up a runnable console application with MAF RC4, full OTel instrumentation, Polly resilience, and a basic OrchestratorAgent completing a multi-turn session.

| Story ID | Title | Complexity | Status | Notes |
|----------|-------|------------|--------|-------|
| US-01 | Solution scaffold and MAF RC4 wiring | M | Done | |
| US-02 | OpenTelemetry + Jaeger observability baseline | M | Done | |
| US-03 | Polly resilience on all outbound calls | S | To Do | Depends on US-01, US-02 |
| US-04 | OrchestratorAgent with session lifecycle | M | To Do | Depends on US-01, US-02, US-03 |
| US-06 | Message history reduction pipeline | S | To Do | Depends on US-04 |

---

## Sprint 2 (Upcoming)
**Goal:** Establish TripPlan structured state, PostgreSQL JSONB session persistence, and 7-agent routing topology.

| Story ID | Title | Complexity |
|----------|-------|------------|
| US-07 | TripPlan structured state (L2) | M |
| US-08 | PostgreSQL JSONB session store (L3) | L |
| US-09 | Session serialization / snapshot versioning | S |
| US-10 | 30-day TTL and session expiry | S |
| US-05 | AgentWorkflowBuilder routing and handoff | M |

---

## Sprint 3 (Upcoming)
**Goal:** Deliver Qdrant RAG pipeline and wire CurrencyAgent, WeatherAgent, and CultureAgent.

| Story ID | Title | Complexity |
|----------|-------|------------|
| US-11 | Qdrant vector store + embedding pipeline | L |
| US-12 | RAG context provider factory and citation | M |
| US-13 | CurrencyAgent with exchange rate RAG | M |
| US-14 | WeatherAgent with climate/forecast RAG | M |
| US-15 | CultureAgent with destination knowledge RAG | M |

---

## Sprint 4 (Upcoming)
**Goal:** Deliver FlightAgent and HotelAgent with console approval, workflow checkpointing, and booking state in TripPlan.

| Story ID | Title | Complexity |
|----------|-------|------------|
| US-18 | Human-in-the-loop console approval flow | M |
| US-19 | Workflow checkpointing at booking graph edges | M |
| US-16 | FlightAgent with booking approval | L |
| US-17 | HotelAgent with booking approval | L |

---

## Sprint 5 (Upcoming)
**Goal:** Complete ItineraryAgent, deliver full trip summary, validate OTel span coverage.

| Story ID | Title | Complexity |
|----------|-------|------------|
| US-20 | ItineraryAgent with structured day-by-day output | L |
| US-21 | Full TripPlan assembly and trip summary | M |
| US-23 | Observability span validation and trace coverage | M |

---

## Sprint 6 (Upcoming)
**Goal:** End-to-end integration testing, Polly resilience hardening, and MVP release certification.

| Story ID | Title | Complexity |
|----------|-------|------------|
| US-22 | End-to-end scenario testing with Testcontainers | L |
| US-24 | Polly circuit-breaker integration tests | M |

---

## Completed Sprints

_None yet._
