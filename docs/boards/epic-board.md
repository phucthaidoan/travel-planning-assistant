# Epic Board — Intelligent Travel Planning Assistant

> Update this file when an epic's status changes. Statuses: `Not Started` | `In Progress` | `Done`

| Epic ID | Epic Name | Status | Notes |
|---------|-----------|--------|-------|
| EP-01 | Foundation & Infrastructure | Not Started | Solution scaffold, MAF RC4, OTel + Jaeger, Docker Compose, Polly |
| EP-02 | Orchestration & Workflow | Not Started | OrchestratorAgent, AgentWorkflowBuilder, session lifecycle, history reduction |
| EP-03 | Durable Session Persistence (L3) | Not Started | Raw Npgsql JSONB store, serialize/deserialize, versioning, TTL |
| EP-04 | Structured State (L2) | Not Started | `ProviderSessionState<TripPlan>`, factory delegates, state validation |
| EP-05 | RAG Knowledge Layer (L1) | Not Started | Qdrant, TextSearchProvider per agent, text-embedding-3-small, citation |
| EP-06 | Informational Specialist Agents | Not Started | CurrencyAgent, WeatherAgent, CultureAgent |
| EP-07 | Booking Agents & Human-in-the-Loop | Not Started | FlightAgent, HotelAgent, ApprovalRequiredAIFunction, checkpointing |
| EP-08 | ItineraryAgent & Trip Assembly | Not Started | ItineraryAgent, full TripPlan assembly, trip summary |
| EP-09 | Integration, Testing & Stabilization | Not Started | End-to-end Testcontainers, OTel span validation, Polly hardening |
