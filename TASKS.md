# Project Tasks

Current state: backend on .NET 10, admin portal complete, public search UI complete, PDF file storage + inline viewer, 86 tests passing, full docker-compose stack (postgres + backend + admin + user frontend).

---

## Completed

All core features shipped:

- Public Search UI (`frontend/user/`, port 3001) — Georgian-only, desktop, light theme, no auth
- Three search modes: keyword, semantic, RAG streaming
- Document list page with pagination and preview modal
- Document detail page with embedded PDF viewer (falls back to raw text)
- PDF file storage on upload, served inline via `/api/documents/{id}/file` and `/api/admin/documents/{id}/file`
- Search result deduplication (DISTINCT ON), title included in keyword search
- Search results link to document detail with chunk highlighting
- Admin PDF viewer on document detail page
- Docker Compose with uploads volume persistence
- CORS configured for both frontends

---

## Known Issues

- **PDF upload path coupling**: FilePath stored as absolute path — if container path changes, existing file references break
- **No file cleanup on document delete**: deleting a document doesn't remove the PDF from disk
- **RAG streaming**: no error handling if SSE connection drops mid-stream
- **Search highlight regex**: `regex.test()` with `g` flag has stateful `lastIndex` — can cause alternating match/miss on identical words

---

## Improvements (Not Started)

### Backend
- [ ] Delete uploaded PDF file when document is deleted
- [ ] Store relative file paths instead of absolute
- [ ] Add `GET /api/documents/{id}/file` content-disposition filename for download (currently no filename)
- [ ] Add search result pagination (currently hard limit)
- [ ] Rate limiting on public search endpoints

### Frontend (User)
- [ ] Mobile responsive layout
- [ ] Dark theme support
- [ ] RAG source citations clickable → document detail
- [ ] Search URL sync (shareable search links via query params)
- [ ] Empty state illustrations

### Frontend (Admin)
- [ ] Loading skeletons and error boundaries
- [ ] Configurable chunking parameters in upload form
- [ ] Bulk document operations (delete multiple)

### Testing
- [ ] E2E tests (Playwright) for both frontends
- [ ] Integration tests for file upload/download pipeline
- [ ] Georgian text search edge cases (integration tests)
- [ ] Performance/load tests (k6 or NBomber)

### Observability
- [ ] Structured logging (Serilog)
- [ ] Search analytics (query logging, popular searches)
- [ ] Failed ingestion alerting
