# Project Tasks

Current state: backend on .NET 10, admin portal complete, 86 tests passing, full docker-compose stack.

---

## Public Search UI — Development Plan

Separate Next.js app at `frontend/user/`, port 3001, Georgian-only, desktop, light theme, fully public (no auth).

### Phase 1: Backend — Public Document Endpoints

- [x] **1.1** Add `GET /api/documents` — paginated list of **Ready** documents (title, chunk count, id, uploadedAt). Public, no auth.
- [x] **1.2** Add `GET /api/documents/{id}` — document metadata (title, source law, dates, chunk count). Ready only.
- [ ] ~~**1.3** Add `GET /api/documents/{id}/file` — deferred, no file storage yet.~~
- [x] **1.4** Add CORS origin `http://localhost:3001` to appsettings + docker-compose env.
- [x] **1.5** Wire new endpoints into `UserEndpoints.cs` under `/api` group.

### Phase 2: Frontend — Project Scaffolding

- [x] **2.1** Init Next.js 16 project at `frontend/user/` (React 19, TypeScript, Tailwind CSS 4).
- [x] **2.2** Set up `globals.css`, base layout, Georgian font support.
- [x] **2.3** Create API client (`lib/api.ts`) — `NEXT_PUBLIC_API_URL` default `http://localhost:5081`.
- [x] **2.4** Add `Dockerfile` (mirror admin pattern).
- [x] **2.5** Add `CLAUDE.md` / `AGENTS.md` with Next.js 16 warning.

### Phase 3: Frontend — Layout & Navigation

- [x] **3.1** Root layout — light theme, clean header with app title + nav links.
- [x] **3.2** Two pages: **ძიება** (Search, home `/`) and **დოკუმენტები** (Documents, `/documents`).
- [x] **3.3** Desktop-only styling.

### Phase 4: Frontend — Documents Page

- [x] **4.1** `/documents` page — paginated table of documents.
- [x] **4.2** Columns: სათაური (Title), ნაწილები (Chunks), მოქმედებები (Actions).
- [x] **4.3** Actions: eye button → preview modal, link button → document detail/PDF.
- [x] **4.4** Document preview modal — metadata (title, source law, dates, status).
- [x] **4.5** Pagination controls.

### Phase 5: Frontend — Search Page

- [x] **5.1** Search page (`/`) — input + mode switcher.
- [x] **5.2** Three modes via tabs: **საკვანძო სიტყვა** (Keyword), **სემანტიკური** (Semantic), **RAG**.
- [x] **5.3** Keyword & Semantic results: cards with document title, article number, chunk text with highlight, score.
- [x] **5.4** RAG mode: streaming toggle, AI answer area with token-by-token render, source articles below.
- [x] **5.5** Result cards: "გახსნა" button → open PDF in new tab.
- [x] **5.6** Loading/empty/error states (Georgian text).

### Phase 6: Docker Integration

- [x] **6.1** Add `user-frontend` service to `docker-compose.yml` — port 3001, depends on backend.
- [x] **6.2** Update start/stop skills if needed.

### Phase 7: Polish

- [x] **7.1** Search highlight — bold/yellow matched terms in chunks.
- [x] **7.2** Latency display — "მოიძებნა X შედეგი, Y მწ".
- [x] **7.3** SSE streaming UX — typing animation, auto-scroll.
- [x] **7.4** Georgian typography consistency.

---

**~24 tasks, 7 phases.** Phases 1 & 2 parallelizable. Phase 6 after Phase 2. Rest sequential.

---

## Other Tasks (Lower Priority)

### Testing Gaps
- E2E tests (Playwright) for both frontends
- Expanded integration tests (Georgian text, SSE format, large PDFs)
- Performance/load tests (k6 or NBomber)

### Observability
- Structured logging (Serilog)
- Search analytics (query logging)
- Failed ingestion alerting

### Polish (Admin)
- Loading skeletons, error boundaries, logout cleanup
- Embedding dimension validation
- Configurable chunking parameters
