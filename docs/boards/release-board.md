# Release Board — Intelligent Travel Planning Assistant

> Tracks MVP release readiness. Update after each sprint's stabilization gate.

---

## MVP Release Gate Checklist

| Gate | Owner Story | Status | Sprint |
|------|-------------|--------|--------|
| Solution builds and runs end-to-end | US-01 | Done | Sprint 1 |
| OTel spans visible in Jaeger | US-02 | Done | Sprint 1 |
| Polly retry + circuit-breaker wired | US-03 | Done | Sprint 1 |
| OrchestratorAgent multi-turn session works | US-04 | Done | Sprint 1 |
| History reduction prevents token overflow | US-06 | Not Started | Sprint 1 |
| TripPlan structured state (L2) validated | US-07 | Not Started | Sprint 2 |
| Sessions persist and resume across restarts | US-08 | Not Started | Sprint 2 |
| Session snapshot versioning in place | US-09 | Not Started | Sprint 2 |
| 30-day TTL expiry working | US-10 | Not Started | Sprint 2 |
| All 7 routing edges wired in workflow | US-05 | Not Started | Sprint 2 |
| Qdrant RAG search returns grounded results | US-11 | Not Started | Sprint 3 |
| Per-agent TextSearchProvider with citation | US-12 | Not Started | Sprint 3 |
| CurrencyAgent grounded response | US-13 | Not Started | Sprint 3 |
| WeatherAgent grounded response | US-14 | Not Started | Sprint 3 |
| CultureAgent grounded response | US-15 | Not Started | Sprint 3 |
| Console approval flow (Y/N) working | US-18 | Not Started | Sprint 4 |
| Workflow checkpoint + resume working | US-19 | Not Started | Sprint 4 |
| FlightAgent booking + approval confirmed | US-16 | Not Started | Sprint 4 |
| HotelAgent booking + approval confirmed | US-17 | Not Started | Sprint 4 |
| Day-by-day itinerary generated | US-20 | Not Started | Sprint 5 |
| Full trip summary produced | US-21 | Not Started | Sprint 5 |
| OTel span coverage validated across all agents | US-23 | Not Started | Sprint 5 |
| End-to-end Testcontainers scenario passes | US-22 | Not Started | Sprint 6 |
| Polly resilience hardening verified | US-24 | Not Started | Sprint 6 |

---

## Release Status

| Version | Date | Status | Notes |
|---------|------|--------|-------|
| MVP v1.0 | TBD | Not Ready | All 6 sprints must complete |

---

## Post-MVP Roadmap

| Item | Story ID | Target |
|------|----------|--------|
| MessagePack binary serialization | US-25 | v1.1 |
| Qdrant Cloud deployment | US-26 | v1.1 |
| Web / REST API front-end | US-28 | v1.2 |
| Multi-tenant session isolation | US-27 | v1.2 |
| SummarizationCompactionStrategy | US-30 | v1.1 |
| Automated RAG ingestion pipeline | US-31 | v1.1 |
| Cross-trip user preference profiles | US-32 | v1.3 |
| Semantic Kernel secondary layer | US-29 | v1.3 |
