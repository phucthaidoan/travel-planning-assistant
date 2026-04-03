# Agile Delivery Plan — Intelligent Travel Planning Assistant
**Date:** 2026-04-01
**Domain:** Intelligent Travel Planning
**Framework:** Microsoft Agent Framework (MAF) RC4 — `Microsoft.Agents.AI.OpenAI` v1.0.0-rc4
**Requirements Source:** `docs/maf-conversation-memory-requirements.md`

---

## Context

The goal is to deliver a production-oriented MVP of an AI-powered Travel Planning Assistant built on MAF RC4. The assistant uses a multi-agent architecture (Orchestrator + 6 specialists) with three memory tiers (RAG, structured state, durable persistence), full OpenTelemetry observability, Polly resilience, and human-in-the-loop booking approval via console Y/N prompt.

The PRD explicitly mandates all three memory layers, all seven agents, observability from Sprint 1, and durable cross-session persistence as MVP scope. Post-MVP is limited to genuine enhancements (cloud deployment, binary serialization, multi-tenancy, web frontend).

**Persistence approach confirmed:** Pure raw Npgsql ADO.NET throughout — no EF Core. Based on the existing `SupportBotV4b.cs` playground pattern, fixed for three known bugs (JSON deserialization error handling, `$type` discriminator brittleness, optimistic locking). This can be migrated to EF Core in a later sprint if needed.

---

## A. Epic Breakdown

| Epic ID | Epic Name | Description |
|---------|-----------|-------------|
| EP-01 | Foundation & Infrastructure | Solution scaffold, MAF RC4 pinning (`global.json`), OpenTelemetry + Jaeger, Docker Compose (PostgreSQL, Qdrant, Jaeger), Polly resilience baseline |
| EP-02 | Orchestration & Workflow | OrchestratorAgent, `AgentWorkflowBuilder` routing, session lifecycle (`CreateSessionAsync` / `RunAsync`), message history reduction |
| EP-03 | Durable Session Persistence (L3) | Raw Npgsql JSONB session store keyed by `(UserId, TripId)`, `SerializeSession` / `DeserializeSessionAsync`, snapshot versioning, 30-day TTL |
| EP-04 | Structured State (L2) | `ProviderSessionState<TripPlan>`, typed `TripPlan` record, factory delegates, state validation on write |
| EP-05 | RAG Knowledge Layer (L1) | Qdrant vector store, `TextSearchProvider` per agent, `text-embedding-3-small`, source citation, context provider deduplication |
| EP-06 | Informational Specialist Agents | CurrencyAgent, WeatherAgent, CultureAgent — RAG-only specialists wired into the workflow |
| EP-07 | Booking Agents & Human-in-the-Loop | FlightAgent, HotelAgent, `ApprovalRequiredAIFunction`, console Y/N prompt, workflow checkpointing |
| EP-08 | ItineraryAgent & Trip Assembly | ItineraryAgent, full TripPlan assembly, consolidated trip summary |
| EP-09 | Integration, Testing & Stabilization | End-to-end Testcontainers suite, OTel span validation, Polly circuit-breaker hardening |

---

## B. MVP vs Post-MVP Scope

| Scope | Story ID | Story Title | Reason |
|-------|----------|-------------|--------|
| MVP | US-01 | Solution scaffold and MAF RC4 wiring | Unblocks all work; required foundation |
| MVP | US-02 | OpenTelemetry + Jaeger observability baseline | Mandatory from Sprint 1 per PRD |
| MVP | US-03 | Polly resilience on all outbound calls | REQ-CC-03; applies to every agent from day one |
| MVP | US-04 | OrchestratorAgent with session lifecycle | Core routing agent; all specialists depend on it |
| MVP | US-05 | AgentWorkflowBuilder routing and handoff | Required before any specialist is complete |
| MVP | US-06 | Message history reduction pipeline | Prevents token overflow in multi-turn sessions |
| MVP | US-07 | TripPlan structured state (L2) | Shared state underpins all agents |
| MVP | US-08 | PostgreSQL JSONB session store (L3) | Durable persistence is MVP per PRD constraints |
| MVP | US-09 | Session serialization / snapshot versioning | Cross-session continuity + schema safety |
| MVP | US-10 | 30-day TTL and session expiry | Required for L3 completeness |
| MVP | US-11 | Qdrant vector store + embedding pipeline | L1 RAG foundation for all specialist agents |
| MVP | US-12 | RAG context provider factory and citation | REQ-CC-01 and REQ-CC-04 compliance |
| MVP | US-13 | CurrencyAgent with exchange rate RAG | Core domain specialist |
| MVP | US-14 | WeatherAgent with climate/forecast RAG | Core domain specialist |
| MVP | US-15 | CultureAgent with destination knowledge RAG | Core domain specialist |
| MVP | US-16 | FlightAgent with booking approval | Human-in-the-loop booking flow |
| MVP | US-17 | HotelAgent with booking approval | Human-in-the-loop booking flow |
| MVP | US-18 | Human-in-the-loop console approval flow | `ApprovalRequiredAIFunction` — explicit MVP requirement |
| MVP | US-19 | Workflow checkpointing at booking graph edges | Long-running booking safety |
| MVP | US-20 | ItineraryAgent with structured day-by-day output | Core deliverable of the assistant |
| MVP | US-21 | Full TripPlan assembly and trip summary | Closes the user-facing feature loop |
| MVP | US-22 | End-to-end scenario testing with Testcontainers | Integration gate for release confidence |
| MVP | US-23 | Observability span validation and trace coverage | OTel GenAI convention compliance |
| MVP | US-24 | Polly circuit-breaker integration tests | Resilience verification |
| Post-MVP | US-25 | MessagePack binary session serialization | Performance optimization; JSON satisfies MVP |
| Post-MVP | US-26 | Qdrant Cloud remote deployment | Operational concern; Docker satisfies MVP |
| Post-MVP | US-27 | Multi-tenant session isolation and scoped TTL | Single-user console app for MVP |
| Post-MVP | US-28 | Web / REST API front-end | Console entry point satisfies MVP |
| Post-MVP | US-29 | Semantic Kernel as secondary AI layer | REQ-CC-04 explicitly defers SK as primary |
| Post-MVP | US-30 | SummarizationCompactionStrategy | Sliding window satisfies MVP; summarization is enhancement |
| Post-MVP | US-31 | Automated RAG document ingestion pipeline | Static seed script satisfies MVP |
| Post-MVP | US-32 | Cross-trip user preference profiles | Per-trip TripPlan state is sufficient for MVP |

---

## C. Full Backlog

### US-01 — Solution scaffold and MAF RC4 wiring
- **Epic:** EP-01
- **Title:** As a developer, I want a solution scaffold with MAF RC4 packages pinned in `global.json` and a runnable console entry point so that the team has a consistent, reproducible starting point.
- **Acceptance Criteria:**
  1. `global.json` pins `Microsoft.Agents.AI.OpenAI` v1.0.0-rc4; `dotnet restore` succeeds with pinned versions.
  2. Console application starts, prints a startup banner, accepts user input, and exits cleanly.
  3. `appsettings.json` contains placeholder sections for Azure OpenAI endpoint, PostgreSQL connection string, Qdrant URL, and Jaeger endpoint.
  4. Docker Compose defines services for PostgreSQL, Qdrant, and Jaeger with health checks.
  5. `dotnet build` and `dotnet run` succeed from a clean clone with only Docker prerequisites installed.
- **Complexity:** M
- **Dependencies:** none

### US-02 — OpenTelemetry + Jaeger observability baseline
- **Epic:** EP-01
- **Title:** As an operator, I want every agent invocation and outbound API call instrumented with OpenTelemetry spans so that I can trace the full pipeline in Jaeger from day one.
- **Acceptance Criteria:**
  1. `AddOpenTelemetry()` configured with GenAI semantic conventions; spans appear in Jaeger after a single user interaction.
  2. Each agent activity span includes `AgentSession ID` as a trace attribute.
  3. Outbound HTTP calls (OpenAI, Qdrant) produce child spans linked to the parent agent span.
  4. Smoke test confirms at least one complete trace in Jaeger.
  5. No agent invocation completes without emitting a span (asserted by span count > 0 in integration test).
- **Complexity:** M
- **Dependencies:** US-01

### US-03 — Polly resilience on all outbound calls
- **Epic:** EP-01
- **Title:** As a developer, I want all outbound HTTP clients wrapped with Polly retry and circuit-breaker policies so that transient failures do not crash the assistant.
- **Acceptance Criteria:**
  1. Shared `ResiliencePipelineBuilder` applies exponential-backoff retry (3 attempts) and circuit-breaker (5 failures / 30s break) to all `HttpClient` registrations.
  2. Injecting 503 from a mock server triggers retry logging and eventual circuit-breaker open state without unhandled exception.
  3. All `IHttpClientFactory`-registered clients (OpenAI, Qdrant) inherit the shared policy.
  4. Polly events (retry attempt, circuit opened/closed) emit OpenTelemetry events on the active span.
  5. Unit test covers: successful call, retry-then-success, circuit-breaker open scenario.
- **Complexity:** S
- **Dependencies:** US-01, US-02

### US-04 — OrchestratorAgent with session lifecycle
- **Epic:** EP-02
- **Title:** As a user, I want the console assistant to route my travel query to an OrchestratorAgent that manages my session so that my conversation persists across multiple turns.
- **Acceptance Criteria:**
  1. `OrchestratorAgent` constructed via `AgentWorkflowBuilder` with at least one routing edge registered.
  2. `CreateSessionAsync` produces a session with unique `SessionId`; `RunAsync` executes the 9-step pipeline.
  3. Two sequential messages in the same session retain context from the first in the second response.
  4. Console loop reads user input, calls `RunAsync`, prints reply in a continuous loop.
  5. OTel span for orchestrator invocation includes `AgentSession ID` and `routing_target` attribute.
- **Complexity:** M
- **Dependencies:** US-01, US-02, US-03

### US-05 — AgentWorkflowBuilder routing and handoff
- **Epic:** EP-02
- **Title:** As a developer, I want `AgentWorkflowBuilder` routing edges defined for all seven agents so that the OrchestratorAgent can delegate to the correct specialist at runtime.
- **Acceptance Criteria:**
  1. Routing edges defined for: Currency, Weather, Flight, Hotel, Culture, Itinerary, and Orchestrator return edge.
  2. Currency-related intent routes to `CurrencyAgent` (stub acceptable for this story).
  3. Routing decisions logged as OTel span events with `intent` and `target_agent` attributes.
  4. Unrecognized intent falls back to Orchestrator with a clarifying prompt rather than throwing.
  5. Integration test exercises at least three routing paths and asserts correct agent was invoked.
- **Complexity:** M
- **Dependencies:** US-04

### US-06 — Message history reduction pipeline
- **Epic:** EP-02
- **Title:** As a user, I want long conversations to remain coherent without exceeding token limits so that the assistant does not degrade or fail after many turns.
- **Acceptance Criteria:**
  1. `MessageCountingChatReducer` (or `IChatReducer`) registered and enforces `RecentMessageMemoryLimit` ≥ 4 per agent.
  2. Test with 20 synthetic messages confirms context window never exceeds configured limit.
  3. History reduction fires transparently — user receives coherent response after reduction without error.
  4. Reduction events emit OTel span event with `messages_before` and `messages_after` attributes.
  5. Message limit externalized to `appsettings.json`.
- **Complexity:** S
- **Dependencies:** US-04

### US-07 — TripPlan structured state (L2)
- **Epic:** EP-04
- **Title:** As a developer, I want a typed `TripPlan` record stored in `ProviderSessionState` so that all agents read and write structured trip data from a single source of truth.
- **Acceptance Criteria:**
  1. `TripPlan` record contains: Destination, TravelDates (check-in/check-out), Budget, PreferredAirline, DietaryRestrictions, ActivityPreferences, `List<ItineraryDay>`.
  2. `ProviderSessionState<TripPlan>` registered per-session via factory delegate (REQ-CC-01); no singleton instance.
  3. `StateKeys` constants file defines the retrieval key.
  4. State validation guard rejects writes where Destination is null/empty or TravelDates are in the past; errors returned, not thrown.
  5. Unit test covers: create, read, update a field, and reject invalid state — all without a live LLM.
- **Complexity:** M
- **Dependencies:** US-04

### US-08 — PostgreSQL JSONB session store (L3)
- **Epic:** EP-03
- **Title:** As a user, I want my trip planning session saved to PostgreSQL so that I can resume a conversation after restarting the application.
- **Acceptance Criteria:**
  1. `PostgreSqlChatHistoryProvider` (raw Npgsql, based on fixed V4b pattern) stores session data as JSONB in a `chat_history` table keyed by `(UserId, TripId)`.
  2. JSON deserialization wrapped in try-catch; corrupt row logs a warning and returns empty session (no crash).
  3. `ChatHistoryJsonNormalizer.EnsurePolymorphicDiscriminatorFirst()` applied consistently before every write.
  4. After `SerializeSession()`, the session row exists in PostgreSQL with a valid JSON payload; `DeserializeSessionAsync` rehydrates and continues conversation.
  5. Testcontainers spins up real PostgreSQL (no mocking); integration test confirms full round-trip.
- **Complexity:** L
- **Dependencies:** US-04, US-07

### US-09 — Session serialization / snapshot versioning
- **Epic:** EP-03
- **Title:** As a developer, I want serialized sessions to carry a schema version so that model evolution does not corrupt existing saved sessions.
- **Acceptance Criteria:**
  1. Each session row includes a `schema_version` integer column; current version is `1`.
  2. Deserializer encountering an unknown version logs a warning and returns a new empty session rather than throwing.
  3. A migration helper upgrades a v0 (legacy) payload to v1 in a unit test.
  4. `SerializeSession()` always writes current `schema_version`; no row saved without version field.
  5. Two unit tests: same-version roundtrip and version-mismatch graceful degradation.
- **Complexity:** S
- **Dependencies:** US-08

### US-10 — 30-day TTL and session expiry
- **Epic:** EP-03
- **Title:** As an operator, I want sessions older than 30 days automatically expired so that the database does not accumulate stale trip data.
- **Acceptance Criteria:**
  1. `chat_history` table includes `last_accessed_utc` column updated on every `SerializeSession()` call.
  2. Background cleanup service (or scheduled Npgsql query) deletes rows where `last_accessed_utc < UtcNow - 30 days`.
  3. `DeserializeSessionAsync` returns null (not exception) for expired/missing session; console app starts new session gracefully.
  4. Integration test: row with `last_accessed_utc` 31 days in the past is deleted by cleanup.
  5. TTL value externalized to `appsettings.json` with default of 30 days.
- **Complexity:** S
- **Dependencies:** US-08

### US-11 — Qdrant vector store + embedding pipeline
- **Epic:** EP-05
- **Title:** As a developer, I want Qdrant running in Docker with a `text-embedding-3-small` embedding pipeline so that knowledge documents can be ingested and retrieved by all agents.
- **Acceptance Criteria:**
  1. Docker Compose includes Qdrant service; seed script creates named collections for each agent domain (currency, weather, flight, hotel, culture).
  2. Ingestion utility reads plain-text files from `data/` folder, generates embeddings via `text-embedding-3-small`, upserts into appropriate Qdrant collection.
  3. Search against currency collection for "USD to EUR" returns at least one result with score > 0.7.
  4. Embedding calls wrapped with Polly and emit OTel spans.
  5. Integration test: ingest one document, query it, assert original text appears in result payload.
- **Complexity:** L
- **Dependencies:** US-03, US-02

### US-12 — RAG context provider factory and citation
- **Epic:** EP-05
- **Title:** As a developer, I want each agent to have its own `TextSearchProvider` wired with `BeforeAIInvoke` behavior and source citation so that LLM responses are grounded in domain knowledge.
- **Acceptance Criteria:**
  1. `TextSearchProvider` instantiated per agent via factory delegate; no shared singleton.
  2. `TextSearchBehavior.BeforeAIInvoke` set on every agent; retrieved chunks injected before LLM call.
  3. `.WithAIContextProviderMessageRemoval()` applied when both RAG and `ChatHistoryProvider` are active on the same agent.
  4. Every RAG-grounded response includes at least one source citation (e.g., `[Source: <document_name>]`).
  5. Unit test with mocked `TextSearchProvider` confirms context injected before LLM stub is called.
- **Complexity:** M
- **Dependencies:** US-11, US-04

### US-13 — CurrencyAgent with exchange rate RAG
- **Epic:** EP-06
- **Title:** As a user, I want to ask about currency exchange rates and policies so that I can plan my budget accurately for my destination.
- **Acceptance Criteria:**
  1. `CurrencyAgent` routed to for currency-related intents (e.g., "How much is 500 USD in EUR?").
  2. Agent retrieves exchange-rate documents from Qdrant via `TextSearchProvider` before LLM call.
  3. Response includes source citation from knowledge base.
  4. TripPlan Budget field can be updated by CurrencyAgent when user provides a foreign-currency budget (converted to base currency).
  5. End-to-end test with seeded Qdrant data confirms grounded, cited response.
- **Complexity:** M
- **Dependencies:** US-05, US-07, US-12

### US-14 — WeatherAgent with climate/forecast RAG
- **Epic:** EP-06
- **Title:** As a user, I want to ask about weather and climate at my destination so that I can pack appropriately and choose optimal travel dates.
- **Acceptance Criteria:**
  1. `WeatherAgent` routed to for weather-related intents; routing test covers "What's the weather like in Tokyo in March?"
  2. Agent queries Qdrant climate/forecast collection; context injected before LLM call.
  3. Response scoped to `TripPlan.Destination` if set; includes source citation.
  4. `TripPlan.TravelDates` can be suggested or confirmed by WeatherAgent based on optimal climate windows.
  5. Multi-turn test: weather query → follow-up packing question demonstrates context retention.
- **Complexity:** M
- **Dependencies:** US-05, US-07, US-12

### US-15 — CultureAgent with destination knowledge RAG
- **Epic:** EP-06
- **Title:** As a user, I want to ask about cultural norms, local customs, and destination highlights so that I can plan a respectful and enriching trip.
- **Acceptance Criteria:**
  1. `CultureAgent` handles intents for customs, etiquette, local laws, and attractions.
  2. Knowledge base seeded for at least two destinations; retrieval returns domain-relevant chunks.
  3. Source citations included in every culture-related response.
  4. `ActivityPreferences` in TripPlan can be updated when user expresses interest in specific activities.
  5. If no destination set in TripPlan, CultureAgent prompts user to specify destination before querying.
- **Complexity:** M
- **Dependencies:** US-05, US-07, US-12

### US-16 — FlightAgent with booking approval
- **Epic:** EP-07
- **Title:** As a user, I want to search for available flights and receive a recommendation grounded in airline policy so that I can make an informed booking decision.
- **Acceptance Criteria:**
  1. `FlightAgent` retrieves airline route and policy documents from Qdrant; presents options aligned with `TripPlan.PreferredAirline` if set.
  2. Recommendation includes route, estimated price range, and source citation.
  3. User selecting a flight triggers `ApprovalRequiredAIFunction`; execution blocks until console Y/N received.
  4. "Y" records selection in TripPlan and emits `approval_granted` OTel span event; "N" re-presents alternatives.
  5. Multi-turn test: search → recommendation → approval prompt → Y → TripPlan updated with flight details.
- **Complexity:** L
- **Dependencies:** US-05, US-07, US-12, US-18, US-19

### US-17 — HotelAgent with booking approval
- **Epic:** EP-07
- **Title:** As a user, I want to search for available hotels and approve a booking so that my accommodation is confirmed within my trip plan.
- **Acceptance Criteria:**
  1. `HotelAgent` retrieves accommodation catalogue from Qdrant; filters by destination and TravelDates from TripPlan.
  2. Recommendation includes name, location, price range, and source citation.
  3. `ApprovalRequiredAIFunction` blocks until console Y/N; "Y" updates TripPlan with hotel details; "N" triggers new search.
  4. Workflow checkpoint written after approval decision (per US-19) so restart resumes at correct state.
  5. End-to-end test: seeded hotel data → search → recommendation → Y approval → `TripPlan.Hotel` confirmed.
- **Complexity:** L
- **Dependencies:** US-05, US-07, US-12, US-18, US-19

### US-18 — Human-in-the-loop console approval flow
- **Epic:** EP-07
- **Title:** As a user, I want a clear Y/N approval prompt before any booking action so that I remain in control of all confirmed reservations.
- **Acceptance Criteria:**
  1. `ApprovalRequiredAIFunction` implemented via `AIFunctionFactory.Create()` and registered on FlightAgent and HotelAgent.
  2. Console prompt clearly states what is being approved (flight or hotel details) before awaiting input.
  3. Any input other than "Y" (case-insensitive) treated as "N" and triggers cancellation path.
  4. Approval function emits OTel span with `approval_type`, `decision`, and `session_id` attributes.
  5. Unit test: "Y" → approval granted; "N" → denied; "maybe" → treated as denial.
- **Complexity:** M
- **Dependencies:** US-04, US-02

### US-19 — Workflow checkpointing at booking graph edges
- **Epic:** EP-07
- **Title:** As a developer, I want booking workflow state checkpointed at every graph edge so that a crash during a long-running booking flow can be resumed without data loss.
- **Acceptance Criteria:**
  1. After each `AgentWorkflowBuilder` routing edge transition in the booking flow, `SerializeSession()` is called and checkpoint written to PostgreSQL.
  2. Simulating process crash after checkpoint and calling `DeserializeSessionAsync` restores workflow to last checkpointed edge.
  3. Checkpoint writes are atomic — failed write does not corrupt previous valid checkpoint.
  4. Checkpoint events emit OTel span events with `checkpoint_edge` and `session_id` attributes.
  5. Integration test: start booking → checkpoint → simulate restart → resume from checkpoint → complete flow.
- **Complexity:** M
- **Dependencies:** US-08, US-09, US-05

### US-20 — ItineraryAgent with structured day-by-day output
- **Epic:** EP-08
- **Title:** As a user, I want the assistant to build a day-by-day itinerary based on my destination, dates, and preferences so that I have a complete travel plan.
- **Acceptance Criteria:**
  1. `ItineraryAgent` reads TripPlan (Destination, TravelDates, ActivityPreferences, DietaryRestrictions) from `ProviderSessionState<TripPlan>` and generates a structured `List<ItineraryDay>`.
  2. Agent uses full message history (`RecentMessageMemoryLimit` configured) to incorporate context from all prior specialist interactions in the session.
  3. Output written back to `TripPlan.Itinerary` via state write with validation.
  4. Console presents itinerary as: Day N — date, activities, meal suggestions, notes.
  5. If TripPlan is missing Destination or TravelDates, ItineraryAgent returns structured error prompting user to provide those fields.
- **Complexity:** L
- **Dependencies:** US-07, US-13, US-14, US-15

### US-21 — Full TripPlan assembly and trip summary
- **Epic:** EP-08
- **Title:** As a user, I want a complete trip summary including flights, hotels, itinerary, and budget breakdown so that I have a single consolidated view of my travel plan.
- **Acceptance Criteria:**
  1. Orchestrator triggers full TripPlan assembly when user requests summary or all required fields are populated.
  2. Summary includes: destination, travel dates, confirmed flight (if approved), confirmed hotel (if approved), day-by-day itinerary, budget estimate, dietary/activity notes.
  3. TripPlan serialized to PostgreSQL immediately after summary generation.
  4. Source citations from specialist agents aggregated in a "References" section.
  5. Multi-agent trace in Jaeger shows complete span tree from Orchestrator through all contributing specialists.
- **Complexity:** M
- **Dependencies:** US-20, US-16, US-17, US-08

### US-22 — End-to-end scenario testing with Testcontainers
- **Epic:** EP-09
- **Title:** As a developer, I want an end-to-end integration test suite that exercises a complete trip planning scenario so that regressions are caught before release.
- **Acceptance Criteria:**
  1. Testcontainers spins up PostgreSQL, Qdrant, and Jaeger; all health-checked before tests run.
  2. Complete scenario: start session → currency query → weather query → culture query → flight approval (Y) → hotel approval (Y) → itinerary → trip summary.
  3. Each step asserts expected TripPlan field is populated in PostgreSQL after the interaction.
  4. Test suite runs in under 5 minutes on a standard developer machine.
  5. All tests are independent and repeatable; no shared mutable state between cases.
- **Complexity:** L
- **Dependencies:** US-21, US-10, US-19

### US-23 — Observability span validation and trace coverage
- **Epic:** EP-09
- **Title:** As an operator, I want OTel span coverage validated across all agents so that I can confirm full observability compliance before release.
- **Acceptance Criteria:**
  1. Test helper queries Jaeger API after each integration run and asserts spans exist for: Orchestrator, all 6 specialists, Qdrant queries, PostgreSQL writes, and approval events.
  2. All spans follow GenAI semantic conventions (required attribute names asserted).
  3. `AgentSession ID` present as trace attribute on every span in a session trace.
  4. Missing span (simulated by disabling an instrument) causes the validation test to fail explicitly.
  5. Span coverage report printed to test output as a summary table.
- **Complexity:** M
- **Dependencies:** US-21, US-02

### US-24 — Polly circuit-breaker integration tests
- **Epic:** EP-09
- **Title:** As an operator, I want Polly resilience behavior verified under simulated failure conditions so that the system's resilience is proven before release.
- **Acceptance Criteria:**
  1. WireMock server replaces OpenAI endpoint; returns 503 for first two requests, then 200 — asserts third call succeeds via retry.
  2. All-503 responses trigger circuit-breaker open; application returns user-friendly error, not unhandled exception.
  3. Circuit-breaker open/close transitions captured as OTel span events.
  4. Recovery test: after break window expires, next call succeeds and circuit closes.
  5. Polly policy config (retry count, break duration, timeout) overridable via test configuration without code changes.
- **Complexity:** M
- **Dependencies:** US-22, US-03, US-23

---

## D. Sprint Plan

### Sprint 1 — Foundation, Observability, and First Runnable Conversation
**Goal:** Stand up a runnable console application with MAF RC4, full OTel instrumentation, Polly resilience, and a basic OrchestratorAgent completing a multi-turn session.

**Rationale:** Produces a working vertical slice on day one — user types a message, receives an AI response. Observability is mandatory from Sprint 1 per PRD. Polly is established early so every future HTTP client inherits the policy automatically.

| Story | Title |
|-------|-------|
| US-01 | Solution scaffold and MAF RC4 wiring |
| US-02 | OpenTelemetry + Jaeger observability baseline |
| US-03 | Polly resilience on all outbound calls |
| US-04 | OrchestratorAgent with session lifecycle |
| US-06 | Message history reduction pipeline |

---

### Sprint 2 — Structured State, Durable Persistence, and Routing
**Goal:** Establish TripPlan structured state, PostgreSQL JSONB session persistence, and 7-agent routing topology so all future agents write to a shared, durable data model.

**Rationale:** L2 and L3 must be in place before any specialist agent is built — both read from and write to TripPlan and the session store. Routing topology defined now means specialists in Sprint 3 slot into a pre-wired workflow without rework.

| Story | Title |
|-------|-------|
| US-07 | TripPlan structured state (L2) |
| US-08 | PostgreSQL JSONB session store (L3) |
| US-09 | Session serialization / snapshot versioning |
| US-10 | 30-day TTL and session expiry |
| US-05 | AgentWorkflowBuilder routing and handoff |

---

### Sprint 3 — RAG Layer and Informational Specialist Agents
**Goal:** Deliver a working RAG pipeline on Qdrant and wire CurrencyAgent, WeatherAgent, and CultureAgent so that users receive grounded, cited responses to informational travel queries.

**Rationale:** RAG foundation (US-11, US-12) is a shared dependency for all six specialist agents; building it here ensures Sprint 4 can focus on agent behavior. Informational agents have no approval flow, making them the right candidates to validate the RAG pattern before the more complex booking agents.

| Story | Title |
|-------|-------|
| US-11 | Qdrant vector store + embedding pipeline |
| US-12 | RAG context provider factory and citation |
| US-13 | CurrencyAgent with exchange rate RAG |
| US-14 | WeatherAgent with climate/forecast RAG |
| US-15 | CultureAgent with destination knowledge RAG |

---

### Sprint 4 — Human-in-the-Loop Booking Agents and Checkpointing
**Goal:** Deliver FlightAgent and HotelAgent with console approval prompts, workflow checkpointing, and booking state written back to TripPlan so users can confirm reservations.

**Rationale:** Human-in-the-loop approval (US-18) is a new capability not yet exercised; built standalone first so both booking agents reuse it cleanly. Workflow checkpointing (US-19) paired here because booking flows are the only long-running graph transitions in the MVP.

| Story | Title |
|-------|-------|
| US-18 | Human-in-the-loop console approval flow |
| US-19 | Workflow checkpointing at booking graph edges |
| US-16 | FlightAgent with booking approval |
| US-17 | HotelAgent with booking approval |

---

### Sprint 5 — ItineraryAgent, Full Trip Assembly, and Span Validation
**Goal:** Complete the ItineraryAgent, deliver the full trip summary, and validate OTel span coverage across all agents.

**Rationale:** ItineraryAgent is intentionally last among specialists because it consumes outputs of all six prior agents via TripPlan. The full trip summary closes the user-facing feature loop. Span validation is included here because all spans first become fully available once all agents are wired.

| Story | Title |
|-------|-------|
| US-20 | ItineraryAgent with structured day-by-day output |
| US-21 | Full TripPlan assembly and trip summary |
| US-23 | Observability span validation and trace coverage |

---

### Sprint 6 — Integration Testing, Resilience Hardening, and Stabilization
**Goal:** Validate the complete system end-to-end with Testcontainers, harden Polly circuit-breaker behavior, and certify MVP release readiness.

**Rationale:** A dedicated stabilization sprint is essential for an MVP spanning 7 agents, 3 memory layers, and 3 infrastructure dependencies. US-22 is the gate criteria for release confidence. US-24 closes the resilience loop with stress-tested circuit-breaker behavior.

| Story | Title |
|-------|-------|
| US-22 | End-to-end scenario testing with Testcontainers |
| US-24 | Polly circuit-breaker integration tests |

---

## E. Final Explanation

### Why Selected Features Qualify as MVP

All 24 MVP stories are traceable to explicit PRD requirements. Durable persistence (L3) and observability are included despite being traditionally non-functional because the PRD explicitly mandates them at MVP scope. No MVP story is purely infrastructural — each delivers a user-visible or operator-verifiable capability.

### What Was Intentionally Excluded

Post-MVP scope is limited to genuine enhancements that do not block any stated MVP goal. MessagePack, Qdrant Cloud, multi-tenancy, web frontend, Semantic Kernel, summarization compaction, automated RAG ingestion pipeline, and cross-trip user profiles are all deferred. The architecture is designed so each post-MVP item is a localized swap or addition, not a structural rework.

### Key Trade-offs

- **Raw Npgsql over EF Core:** Simpler, matches the proven playground pattern. The actual storage is a JSONB blob requiring manual `JsonSerializer` handling regardless of ORM choice. Can be migrated in a later sprint.
- **Vertical slicing over horizontal layering:** Each story wires all layers end-to-end, producing a demonstrable system sooner at the cost of occasional infrastructure rework when later agents uncover edge cases.
- **Static seeded RAG documents over live APIs:** The PRD specifies RAG over a knowledge base; live API integration is a product decision beyond MVP scope.
- **Console approval over API-based HITL:** Console Y/N satisfies the requirement for MVP. A hosted approval flow is a post-MVP API/UX concern.

### Remaining Risks After MVP Release

1. **LLM routing accuracy:** OrchestratorAgent intent classification is LLM-driven; degraded model responses could misroute. Mitigation: routing confidence logging + fallback clarification prompt (addressed in US-05 AC4).
2. **Qdrant chunk quality:** RAG quality depends entirely on seeded document chunking. Poor chunking produces low-relevance retrievals. Establish a retrieval quality baseline metric before Sprint 3 closes.
3. **Token budget under full load:** Seven agents + RAG context + message history approaching model context limits in complex sessions. US-22 provides the first signal on this risk.
4. **PostgreSQL schema evolution:** Snapshot versioning (US-09) protects deserialization, but major TripPlan schema changes post-MVP require migration helpers.
5. **Console approval fragility:** Blocking console read hangs in non-interactive environments (CI, piped input). Must be replaced before any hosted deployment.

---

## Implementation References

| Reference | Purpose |
|-----------|---------|
| `ms-agent-framework-playground/samples/ConversationMemory/V4b_CustomHistoryProvider_Postgres/SupportBotV4b.cs` | Base implementation for US-08 PostgreSQL provider (fix JSON error handling, add optimistic locking) |
| `ms-agent-framework-playground/samples/ConversationMemory/V7_Integration/` | OpenTelemetry + Jaeger wiring reference for US-02 |
| `ms-agent-framework-playground/samples/Recipe/V5_RAG/` | Vector store + RAG pattern reference for US-11, US-12 |
| `ms-agent-framework-playground/samples/SampleUtilities/AgentChatMessageJson.cs` | Required for polymorphic `ChatMessage` serialization in US-08 |
| `ms-agent-framework-playground/samples/SampleUtilities/ChatHistoryJsonNormalizer.cs` | `$type` discriminator normalization required before every PostgreSQL write |
| `ms-agent-framework-playground/samples/CV_Screening/V5_RAG/` | Multi-agent + RAG + tool composition reference for US-13–US-17 |
| `agent-samples/azure/` | Declarative agent YAML reference for OpenAI/Azure provider configuration |
