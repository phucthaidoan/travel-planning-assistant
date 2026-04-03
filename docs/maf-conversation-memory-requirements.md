# Logic Requirements: Conversation & Memory in an AI-Powered Application
### Domain: Intelligent Travel Planning Assistant
### Framework: Microsoft Agent Framework (MAF) — `Microsoft.Agents.AI` RC

---

> **Source basis:** Derived and paraphrased from the following reference projects:
> - [`Azure-Samples/app-service-maf-workflow-travel-agent-dotnet`](https://github.com/Azure-Samples/app-service-maf-workflow-travel-agent-dotnet)
> - [`microsoft/Agent-Framework-Samples`](https://github.com/microsoft/Agent-Framework-Samples)
> - [MAF official documentation — Agent Memory & RAG](https://learn.microsoft.com/en-us/agent-framework/agents/agent-memory)

---

## Domain Rationale

Travel planning is chosen because it naturally exercises the full breadth of agentic memory patterns. A single planning session requires:

- **Long-term retrieval** — policies, destination guides, visa rules (RAG)
- **Short-term working state** — active itinerary being constructed, user preferences within the session (structured state)
- **Cross-session continuity** — a user who started planning a Tokyo trip yesterday must resume it today without re-stating preferences (durable persistence)
- **Multi-agent delegation** — six or more specialist agents (Currency, Weather, Flight, Hotel, Culture, Itinerary) each needing isolated but coordinated memory

This makes it an ideal domain to demonstrate every required memory tier simultaneously, without artificial contrivance.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Travel Planning Application                            │
│                                                                             │
│  User ──▶ [ OrchestratorAgent ]                                             │
│               │                                                             │
│               ├──▶ [ CurrencyAgent ]   ◀── RAG: Exchange Rate Documents    │
│               ├──▶ [ WeatherAgent ]    ◀── RAG: Forecast / Climate Guides  │
│               ├──▶ [ FlightAgent ]     ◀── RAG: Airline / Route Policies   │
│               ├──▶ [ HotelAgent ]      ◀── RAG: Accommodation Catalogues   │
│               ├──▶ [ CultureAgent ]    ◀── RAG: Destination Knowledge Base │
│               └──▶ [ ItineraryAgent ]  ◀── Structured State + Full History │
│                                                                             │
│  Memory Layers:                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ [L1] RAG — TextSearchProvider (vector store, per-agent)             │   │
│  │ [L2] Structured State — ProviderSessionState<TripPlan>              │   │
│  │ [L3] Durable Persistence — AgentSession serialization + ext. store  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Part 1 — Conversation Logic Requirements

### 1.1 Session Lifecycle

**REQ-CONV-01 — Session Creation**
Each user interaction begins by obtaining an `AgentSession` via `await agent.CreateSessionAsync()`. A session is the authoritative container for all turn-level state — conversation messages, context provider outputs, and structured session data. Sessions must never be shared across concurrent users.

**REQ-CONV-02 — Turn Execution**
Each user message is submitted to the session via `agent.RunAsync(session, message)`. The agent executes a nine-step turn pipeline internally. The application layer is not responsible for managing individual pipeline steps; it only provides the message and receives the `AgentRunResponse`.

**REQ-CONV-03 — Multi-Turn Coherence**
The session object accumulates conversation history across turns automatically. The calling code submits only the new user message each turn; all prior exchanges within the session are made available to the LLM transparently through the `ChatHistoryProvider` registered on the agent.

**REQ-CONV-04 — Agent Handoff within a Workflow**
When the Orchestrator delegates to a specialist agent (e.g., WeatherAgent), the relevant subset of session context must be forwarded. Each specialist agent runs within its own scope but may read shared structured state. Handoff is implemented via `AgentWorkflowBuilder` routing edges, not by passing raw message strings between agents.

**REQ-CONV-05 — History Reduction**
As a conversation grows, the accumulated message history must be trimmed to avoid exceeding model context limits. A `MessageCountingChatReducer` (or equivalent `IChatReducer` implementation) must be configured on each agent to enforce a rolling window. The application does not handle this truncation — it is declared as an agent option.

**REQ-CONV-06 — Human-in-the-Loop Approval**
Any action that books or charges (flight booking, hotel reservation) must be wrapped as an `ApprovalRequiredAIFunction`. The agent will pause execution and surface the pending action to the application layer before proceeding. The application layer is responsible for resuming or cancelling the pending run.

---

## Part 2 — Memory Logic Requirements

### 2.1 Layer 1 — RAG: Vector-Backed Long-Term Memory

**REQ-MEM-RAG-01 — Per-Agent Knowledge Scoping**
Each specialist agent is associated with its own domain-specific vector store collection. The CurrencyAgent queries an exchange rate and policy corpus; the WeatherAgent queries a climate and forecast corpus; the CultureAgent queries a destination knowledge base. Cross-agent knowledge bleed is avoided by scoping `TextSearchProvider` instances to individual agent registrations.

**REQ-MEM-RAG-02 — Search Trigger Mode**
All RAG-enabled agents are configured with `TextSearchBehavior.BeforeAIInvoke`. This means the vector search executes on every turn, before the LLM call, injecting relevant document excerpts into the system context automatically. On-demand (function-call mode) is reserved for agents where retrieval is optional and expensive.

**REQ-MEM-RAG-03 — Contextual Search Window**
The `TextSearchProvider` is configured with a `RecentMessageMemoryLimit` of at least 4 (both user and assistant turns). This ensures the search query is constructed from recent conversation context, not just the latest single user message, improving retrieval relevance for multi-turn planning dialogues.

**REQ-MEM-RAG-04 — Source Citation**
All retrieved documents must supply `SourceName` and `SourceLink` metadata. The agent's system prompt instructs it to cite sources when drawing on retrieved content. This is critical in a travel context where policy accuracy (visa rules, airline cancellation terms) must be traceable.

**REQ-MEM-RAG-05 — RAG + History Deduplication**
When an agent has both a `TextSearchProvider` and a `ChatHistoryProvider`, `.WithAIContextProviderMessageRemoval()` must be configured. Without this, injected RAG context messages are re-appended on every subsequent turn, causing history bloat and potential token overflow.

**REQ-MEM-RAG-06 — Vector Store Portability**
The vector store backend must be abstracted via `Microsoft.Extensions.VectorData`. During development, an `InMemoryVectorStore` is sufficient. Production deployment targets a `QdrantVectorStore`. The swap requires only a constructor-level change; the `TextSearchProvider` and all agent code remain unchanged.

**REQ-MEM-RAG-07 — Embedding Strategy**
Document ingestion and query embedding must use the same embedding model. The application is responsible for ensuring that any updates to the embedding model version are applied consistently to both the ingestion pipeline and the live query path. Model version mismatch produces silently degraded retrieval.

---

### 2.2 Layer 2 — Structured Session State

**REQ-MEM-STATE-01 — Typed Trip Plan State**
A strongly-typed `TripPlan` record is stored within the active session using `ProviderSessionState<TripPlan>`. This object accumulates structured data across the session: destination, travel dates, budget, preferred airline, dietary restrictions, activity preferences, and the evolving day-by-day itinerary.

**REQ-MEM-STATE-02 — State Isolation per Session**
Session state is scoped to the individual `AgentSession`. Two concurrent users planning different trips must never share a `TripPlan` instance. Per-session isolation is guaranteed by using `ChatHistoryProviderFactory` (factory pattern) rather than registering a singleton provider.

**REQ-MEM-STATE-03 — Cross-Agent State Sharing within a Workflow**
Specialist agents that run within the same workflow graph may read from shared structured state. For example, the HotelAgent reads the `TripPlan.Destination` and `TripPlan.TravelDates` values set by the Orchestrator at the start of the session. Write access is restricted to the Orchestrator and ItineraryAgent to prevent conflicting updates.

**REQ-MEM-STATE-04 — Lightweight Scratch Data via StateBag**
Ephemeral values that do not belong in the typed `TripPlan` (e.g., intermediate API responses, retry counters, temporary flags) are stored in a `StateBag` keyed dictionary. These values are not persisted beyond the session and are not included in the serialized state snapshot.

**REQ-MEM-STATE-05 — State Validation on Write**
Before persisting any mutation to `TripPlan`, the application layer must validate that the updated values are internally consistent (e.g., check-out date is after check-in date; budget is a positive value). The agent framework does not enforce domain invariants on typed state.

---

### 2.3 Layer 3 — Durable Cross-Session Persistence

**REQ-MEM-PERSIST-01 — Session Serialization**
At the conclusion of each user turn (or at explicit checkpoints for long-running workflows), the application calls `agent.SerializeSession(session)` to produce a serializable snapshot of the session state. This snapshot includes the conversation history, the structured `TripPlan` state, and any pending workflow position.

**REQ-MEM-PERSIST-02 — External Storage**
Serialized session snapshots are written to an external durable store keyed by a stable `UserId + TripId` composite. The recommended storage backend is a relational store (e.g., SQL Server via Entity Framework Core) or a document store, depending on query requirements. The store must support atomic writes to prevent partial-snapshot corruption.

**REQ-MEM-PERSIST-03 — Session Deserialization and Resumption**
When a returning user resumes a trip plan, the application retrieves the stored snapshot by key and calls `await agent.DeserializeSessionAsync(snapshot)` to reconstruct the `AgentSession`. The restored session is functionally identical to the original — the user continues the conversation exactly where they left off without re-stating any prior context.

**REQ-MEM-PERSIST-04 — Snapshot Versioning**
Serialized snapshots must include a schema version identifier. When the `TripPlan` model evolves between application releases, the deserialization path must handle migration from prior snapshot versions. Snapshots from unsupported prior versions must fail gracefully with a user-facing prompt to restart the planning session.

**REQ-MEM-PERSIST-05 — Long-Running Workflow Checkpointing**
For multi-step workflows that exceed a single HTTP request window (e.g., a booking confirmation that awaits an external airline API), the workflow position must be checkpointed using the agent framework's built-in graph checkpointing capability. The application stores the checkpoint alongside the session snapshot. On resumption, the workflow continues from the last saved edge, not from the beginning.

**REQ-MEM-PERSIST-06 — Session Expiry Policy**
Persisted sessions must carry a `LastAccessedUtc` timestamp. Sessions not accessed within a configurable TTL (e.g., 30 days) are eligible for archival or deletion. Expired sessions are not resumed; the user is prompted to start a new planning session.

---

## Part 3 — Memory Tier Decision Guide

| Scenario | Recommended Tier | MAF Mechanism |
|---|---|---|
| Agent needs domain knowledge it wasn't trained on (airline policies, visa rules) | RAG | `TextSearchProvider` + vector store |
| Agent needs to recall what the user said 5 turns ago | In-context history | `ChatHistoryProvider` (auto via session) |
| Itinerary details must be shared between two agents in the same workflow | Structured state | `ProviderSessionState<TripPlan>` |
| Ephemeral scratch data (retry count, temp flags) | Lightweight state | `StateBag` |
| User resumes trip planning the next day | Durable persistence | `agent.SerializeSession()` / `DeserializeSessionAsync()` |
| Booking step requires external API call that may time out | Workflow checkpoint | Graph checkpointing at workflow edge |
| Context window approaches token limit mid-conversation | History reduction | `MessageCountingChatReducer` / `IChatReducer` |
| RAG + history coexist and causing message duplication | Deduplication | `.WithAIContextProviderMessageRemoval()` |
| Dev environment, no infra | In-memory everything | `InMemoryVectorStore` + default session state |
| Production, multi-tenant | Isolated + durable | Factory pattern + external store + Qdrant |

---

## Part 4 — Cross-Cutting Requirements

**REQ-CC-01 — Session-Scoped Provider Factories**
All context providers that carry per-user data (`ChatHistoryProvider`, `ProviderSessionState<T>`) must be registered via factory delegates, never as singletons. Singleton providers cause session data to leak between concurrent users.

**REQ-CC-02 — Observability**
Every agent turn, tool invocation, RAG search, and state mutation must emit OpenTelemetry spans following the GenAI semantic conventions. The `AgentSession` ID must be propagated as a trace attribute to enable end-to-end trace correlation across specialist agents within the same planning workflow.

**REQ-CC-03 — Resilience**
All outbound calls from agents (vector store queries, external travel APIs) must be wrapped with Polly-based retry and circuit-breaker policies via `Microsoft.Extensions.Http.Resilience`. Transient failures in a sub-agent must not corrupt the shared session state.

**REQ-CC-04 — API Layer Discipline**
All agent configuration, session management, and memory provider registration must use native `Microsoft.Agents.AI` RC abstractions. Semantic Kernel abstractions must not be introduced as the primary API layer; they are permissible only as a bridge adapter for vector store integration where no native MAF equivalent exists.

**REQ-CC-05 — NuGet Source Discipline**
During the RC phase, all MAF packages must be built from source (`microsoft/agent-framework` main branch) rather than consumed from NuGet. Three coexisting package generations on NuGet create significant version ambiguity. Package source is pinned in `global.json` and documented in the repository README.

---

## Open Questions / Decisions Deferred to Implementation

1. **State serialization format** — JSON vs. binary for the `TripPlan` snapshot. JSON is recommended for debuggability; binary (MessagePack) for high-throughput multi-tenant scenarios.
2. **Vector store chunking strategy** — chunk size and overlap for destination knowledge base documents is not defined here; requires empirical tuning.
3. **Approval UX** — how `ApprovalRequiredAIFunction` surfaces to the end user (modal, email, push notification) is an application-layer concern outside these requirements.
4. **Embedding model selection** — the specific model (e.g., `text-embedding-3-small` vs. `text-embedding-3-large`) is deferred; the requirement is only that ingestion and query use the same model version.

---

*Requirements Version: 1.0 | Domain: Travel Planning | Framework: MAF RC | Date: 2026-03-30*
