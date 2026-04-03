# Backlog Board — Intelligent Travel Planning Assistant

> Update `Status` and `Sprint` columns as stories are planned, started, and completed.
> Statuses: `Backlog` | `In Progress` | `Done` | `Blocked`

| Story ID | Epic | Title | Complexity | Sprint | Status | Blocked By |
|----------|------|-------|------------|--------|--------|------------|
| US-01 | EP-01 | Solution scaffold and MAF RC4 wiring | M | Sprint 1 | Done | — |
| US-02 | EP-01 | OpenTelemetry + Jaeger observability baseline | M | Sprint 1 | Done | — |
| US-03 | EP-01 | Polly resilience on all outbound calls | S | Sprint 1 | Backlog | US-01, US-02 |
| US-04 | EP-02 | OrchestratorAgent with session lifecycle | M | Sprint 1 | Backlog | US-01, US-02, US-03 |
| US-06 | EP-02 | Message history reduction pipeline | S | Sprint 1 | Backlog | US-04 |
| US-07 | EP-04 | TripPlan structured state (L2) | M | Sprint 2 | Backlog | US-04 |
| US-08 | EP-03 | PostgreSQL JSONB session store (L3) | L | Sprint 2 | Backlog | US-04, US-07 |
| US-09 | EP-03 | Session serialization / snapshot versioning | S | Sprint 2 | Backlog | US-08 |
| US-10 | EP-03 | 30-day TTL and session expiry | S | Sprint 2 | Backlog | US-08 |
| US-05 | EP-02 | AgentWorkflowBuilder routing and handoff | M | Sprint 2 | Backlog | US-04 |
| US-11 | EP-05 | Qdrant vector store + embedding pipeline | L | Sprint 3 | Backlog | US-03, US-02 |
| US-12 | EP-05 | RAG context provider factory and citation | M | Sprint 3 | Backlog | US-11, US-04 |
| US-13 | EP-06 | CurrencyAgent with exchange rate RAG | M | Sprint 3 | Backlog | US-05, US-07, US-12 |
| US-14 | EP-06 | WeatherAgent with climate/forecast RAG | M | Sprint 3 | Backlog | US-05, US-07, US-12 |
| US-15 | EP-06 | CultureAgent with destination knowledge RAG | M | Sprint 3 | Backlog | US-05, US-07, US-12 |
| US-18 | EP-07 | Human-in-the-loop console approval flow | M | Sprint 4 | Backlog | US-04, US-02 |
| US-19 | EP-07 | Workflow checkpointing at booking graph edges | M | Sprint 4 | Backlog | US-08, US-09, US-05 |
| US-16 | EP-07 | FlightAgent with booking approval | L | Sprint 4 | Backlog | US-05, US-07, US-12, US-18, US-19 |
| US-17 | EP-07 | HotelAgent with booking approval | L | Sprint 4 | Backlog | US-05, US-07, US-12, US-18, US-19 |
| US-20 | EP-08 | ItineraryAgent with structured day-by-day output | L | Sprint 5 | Backlog | US-07, US-13, US-14, US-15 |
| US-21 | EP-08 | Full TripPlan assembly and trip summary | M | Sprint 5 | Backlog | US-20, US-16, US-17, US-08 |
| US-23 | EP-09 | Observability span validation and trace coverage | M | Sprint 5 | Backlog | US-21, US-02 |
| US-22 | EP-09 | End-to-end scenario testing with Testcontainers | L | Sprint 6 | Backlog | US-21, US-10, US-19 |
| US-24 | EP-09 | Polly circuit-breaker integration tests | M | Sprint 6 | Backlog | US-22, US-03, US-23 |

---

## Post-MVP Backlog

| Story ID | Title | Reason Deferred |
|----------|-------|-----------------|
| US-25 | MessagePack binary session serialization | Performance optimization; JSON satisfies MVP |
| US-26 | Qdrant Cloud remote deployment | Operational concern; Docker satisfies MVP |
| US-27 | Multi-tenant session isolation and scoped TTL | Single-user console app for MVP |
| US-28 | Web / REST API front-end | Console entry point satisfies MVP |
| US-29 | Semantic Kernel as secondary AI layer | REQ-CC-04 explicitly defers SK as primary |
| US-30 | SummarizationCompactionStrategy | Sliding window satisfies MVP |
| US-31 | Automated RAG document ingestion pipeline | Static seed script satisfies MVP |
| US-32 | Cross-trip user preference profiles | Per-trip TripPlan state sufficient for MVP |
