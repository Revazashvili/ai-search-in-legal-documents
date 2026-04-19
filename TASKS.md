# Project Tasks

Current state: backend fully implemented, admin portal complete, 86 tests passing. Core gap is the public-facing search UI — endpoints exist but no frontend. Tasks are ordered by priority.

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

## 2. Backend Gaps

### 2.1 Pagination on Document List
- `DocumentRepository.ListAsync` returns all documents — no limit/offset
- Add `page` + `pageSize` params (or cursor-based) to `GET /api/admin/documents`
- Update frontend dashboard to handle paginated response
- Affects: `IDocumentRepository`, `DocumentRepository`, `DocumentAdminEndpoints`, admin dashboard page

### 2.2 File Size & Input Validation
- No maximum file size check on upload — large PDFs can OOM the ingestion service
- Add file size limit (e.g. 50MB) in the upload endpoint before extraction
- Add maximum raw text length guard before persisting to DB
- Add maximum query string length for embedding API calls

### 2.3 Startup Validation for AI Config
- If `OpenAI:BaseUrl` or embedding model is misconfigured, failure is silent until first API call
- Add `IStartupFilter` or validation in `AddInfrastructure` that verifies connectivity to Ollama/OpenAI on startup
- Fail fast with a clear error message rather than crashing mid-ingestion

### 2.4 Rate Limiting on Public Endpoints
- `/api/search/*` endpoints are public with no rate limiting
- Each semantic/RAG request triggers an embedding API call
- Add `AddRateLimiter` middleware with a fixed-window or token-bucket policy
- Consider separate limits for keyword (cheap) vs semantic/RAG (expensive)

### 2.5 Health Check Endpoint
- No `/healthz` endpoint for Docker/load balancer liveness checks
- Add `AddHealthChecks()` with a PostgreSQL check (`AddNpgSql`)
- Register in docker-compose `healthcheck` for the backend service (when re-dockerized)

### 2.6 Update CLAUDE.md
- Embedding dimension is documented as 1536 but the migration changed it to 768 (nomic-embed-text)
- Chat model documented as GPT-4o but default is now `llama3.1:8b` via Ollama
- Update to reflect current AI stack

---

## 3. Infrastructure & Deployment

### 3.1 Re-add Backend & Frontend to Docker Compose
- Backend and frontend were removed from docker-compose to fix startup issues
- When .NET 10 SDK is available locally, restore multi-stage Docker builds
- Backend: `mcr.microsoft.com/dotnet/sdk:10.0` → `aspnet:10.0`
- Frontend: node:22-alpine with standalone Next.js output
- Add backend healthcheck dependency on postgres
- Add `host.docker.internal` for Ollama access from within containers

### 3.2 .env.example File
- No `.env.example` exists — onboarding requires reading CLAUDE.md
- Create `.env.example` at repo root documenting:
  - `OPENAI_API_KEY` (if using OpenAI)
  - `NEXT_PUBLIC_API_URL`
  - Any other env vars needed
- Add note in CLAUDE.md pointing to it

### 3.3 Upgrade to .NET 10
- User requested .NET 10 upgrade; reverted because local SDK was 9.x
- Once .NET 10 SDK is installed (`dotnet --version` shows 10.x):
  - Update `Directory.Build.props`: `net9.0` → `net10.0`
  - Update `Directory.Packages.props`: all `9.0.4` → `10.0.x` packages
  - Update `backend/Dockerfile`: `sdk:9.0` → `sdk:10.0`, `aspnet:9.0` → `aspnet:10.0`
  - Run `dotnet build` and fix any breaking changes

---

## 4. Testing Gaps

### 4.1 E2E Tests (Playwright)
- No end-to-end tests for the frontend
- Add Playwright to `frontend/admin`
- Cover: login flow, document upload, status polling, document detail, delete
- Cover: public search (once built)
- Run in CI alongside unit/integration tests

### 4.2 Expanded Integration Tests
- Search repository: test with real Georgian text (`მუხლი`)
- Search repository: test empty result sets
- Search repository: test limit/offset behavior (once pagination is added)
- Upload endpoint: test with a real small PDF fixture
- RAG endpoint: verify SSE event format matches frontend expectations

### 4.3 Performance / Load Tests
- Ingestion pipeline: test with a large document (10k+ tokens) to verify chunking correctness
- Semantic search: test latency with 1000+ chunks in DB
- Consider `k6` or `NBomber` for load testing search endpoints

---

## 5. User Management

### 5.1 Admin User Creation UI
- No way to create admin users from the UI — relies on seeding or direct DB access
- Add a "Users" section to admin sidebar (visible only to a superadmin role)
- Form to create new admin user (email + password)
- Calls ASP.NET Identity register endpoint
- Optional: role-based access (admin vs read-only)

### 5.2 Change Password
- Identity provides endpoints but no UI wires them up
- Add a "Change Password" form in a profile/settings page

---

## 6. Observability

### 6.1 Structured Logging
- Application uses `ILogger<T>` throughout but logs go to console only
- Add `Serilog` with file sink + structured JSON output
- Log key events: document upload, ingestion start/complete/fail, search queries

### 6.2 Search Analytics
- No record of what users search for
- Optionally log search queries + result counts + latency to a DB table or log sink
- Useful for improving the system and understanding usage patterns

### 6.3 Failed Ingestion Alerting
- When `Status = "Failed"`, currently no notification
- Consider a simple admin dashboard widget showing recent failures
- Or email notification via SMTP when ingestion fails

---

## 7. Minor / Polish

### 7.1 Frontend: Loading Skeletons
- Dashboard shows plain "Loading…" text while fetching
- Replace with skeleton loaders for better UX

### 7.2 Frontend: Error Boundaries
- Unhandled React errors crash the page
- Add a top-level error boundary in the dashboard layout

### 7.3 Frontend: Logout State Cleanup
- Sidebar logout calls `logout()` then redirects, but stale state could remain if redirect fails
- Clear local state explicitly before redirect

### 7.4 Embedding Dimension Validation
- `EmbeddingService` batches calls and returns `float[][]`
- No assertion that returned vectors match expected 768 dimensions
- Add a guard: `if (embedding.Length != 768) throw ...` to catch model mismatches early

### 7.5 Configurable Chunking Parameters
- `FixedWindowTokens = 500`, `FixedOverlapTokens = 50`, `ArticleMaxTokens = 800` are hardcoded constants
- Move to `appsettings.json` under `Chunking:*` for easier tuning without recompile
