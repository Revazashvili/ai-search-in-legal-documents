# Project Tasks

Current state: backend on .NET 10, admin portal complete, 86 tests passing, full docker-compose stack. Core gap is the public-facing search UI — endpoints exist but no frontend. Tasks are ordered by priority.

---

## 1. Public Search UI (Frontend)

**Missing entirely.** The three search endpoints exist (`/api/search/keyword`, `/api/search/semantic`, `/api/search/rag`) but there is no user-facing interface to use them.

### 1.1 Search Page — Keyword & Semantic
- New route: `/search` (public, no auth)
- Search input with mode toggle (Keyword / Semantic / RAG)
- Results list: document title, article number, chunk text excerpt, similarity score badge
- Latency display (ms)
- Loading state while fetching
- Empty state when no results

### 1.2 RAG Search Page / Component
- Streaming SSE response rendered token-by-token
- Source citations displayed after streaming completes (`done: true, sources: [...]`)
- Stop/cancel button mid-stream
- Error state if stream fails

### 1.3 Frontend Routing
- Decide: separate `/search` page or tabs on dashboard
- Public routes must not require auth (no auth guard)
- Shared layout/navigation between admin and public areas, or separate shells

---

## 2. Testing Gaps

### 2.1 E2E Tests (Playwright)
- No end-to-end tests for the frontend
- Add Playwright to `frontend/admin`
- Cover: login flow, document upload, status polling, document detail, delete
- Cover: public search (once built)
- Run in CI alongside unit/integration tests

### 2.2 Expanded Integration Tests
- Search repository: test with real Georgian text (`მუხლი`)
- Search repository: test empty result sets
- Search repository: test limit/offset behavior (once pagination is added)
- Upload endpoint: test with a real small PDF fixture
- RAG endpoint: verify SSE event format matches frontend expectations

### 2.3 Performance / Load Tests
- Ingestion pipeline: test with a large document (10k+ tokens) to verify chunking correctness
- Semantic search: test latency with 1000+ chunks in DB
- Consider `k6` or `NBomber` for load testing search endpoints

---

## 3. Observability

### 3.1 Structured Logging
- Application uses `ILogger<T>` throughout but logs go to console only
- Add `Serilog` with file sink + structured JSON output
- Log key events: document upload, ingestion start/complete/fail, search queries

### 3.2 Search Analytics
- No record of what users search for
- Optionally log search queries + result counts + latency to a DB table or log sink
- Useful for improving the system and understanding usage patterns

### 3.3 Failed Ingestion Alerting
- When `Status = "Failed"`, currently no notification
- Consider a simple admin dashboard widget showing recent failures
- Or email notification via SMTP when ingestion fails

---

## 4. Minor / Polish

### 4.1 Frontend: Loading Skeletons
- Dashboard shows plain "Loading…" text while fetching
- Replace with skeleton loaders for better UX

### 4.2 Frontend: Error Boundaries
- Unhandled React errors crash the page
- Add a top-level error boundary in the dashboard layout

### 4.3 Frontend: Logout State Cleanup
- Sidebar logout calls `logout()` then redirects, but stale state could remain if redirect fails
- Clear local state explicitly before redirect

### 4.4 Embedding Dimension Validation
- `EmbeddingService` batches calls and returns `float[][]`
- No assertion that returned vectors match expected 768 dimensions
- Add a guard: `if (embedding.Length != 768) throw ...` to catch model mismatches early

### 4.5 Configurable Chunking Parameters
- `FixedWindowTokens = 500`, `FixedOverlapTokens = 50`, `ArticleMaxTokens = 800` are hardcoded constants
- Move to `appsettings.json` under `Chunking:*` for easier tuning without recompile
