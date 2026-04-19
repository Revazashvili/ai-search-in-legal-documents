# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend (.NET 9)

```bash
# Build
dotnet build backend/LegalDocumentAISearch.sln

# Run API (from repo root)
dotnet run --project backend/src/LegalDocumentAISearch.Api

# Run all tests
dotnet test backend/LegalDocumentAISearch.sln

# Run a single test class or method
dotnet test backend/LegalDocumentAISearch.sln --filter "FullyQualifiedName~ClassName"
dotnet test backend/LegalDocumentAISearch.sln --filter "FullyQualifiedName~ClassName.MethodName"

# EF Core migrations (run from repo root)
dotnet ef migrations add <Name> \
  --project backend/src/LegalDocumentAISearch.Infrastructure \
  --startup-project backend/src/LegalDocumentAISearch.Api

dotnet ef database update \
  --project backend/src/LegalDocumentAISearch.Infrastructure \
  --startup-project backend/src/LegalDocumentAISearch.Api
```

### Frontend (Next.js, admin portal)

```bash
cd frontend/admin
npm run dev      # http://localhost:3000
npm run build
npm run lint
```

### Infrastructure

```bash
docker compose up --build   # rebuild all images and start postgres + backend + frontend
```

Set `OPENAI_API_KEY` env var before starting (or it uses a placeholder):
```bash
export OPENAI_API_KEY=sk-...
docker compose up --build
```

EF migrations run automatically on backend startup via `MigrateAsync()`.

## Configuration

Before running the API, set the OpenAI API key in `backend/src/LegalDocumentAISearch.Api/appsettings.json` (field `OpenAI:ApiKey`) or via environment variable `OpenAI__ApiKey`. The placeholder value `"YOUR_OPENAI_API_KEY_HERE"` will cause startup to throw.

Postgres connection: `Host=localhost;Port=5432;Database=legaldocumentaisearch;Username=postgres;Password=postgres`

API docs (development only): `http://localhost:5081/scalar`

## Architecture

### Solution layout

```
backend/
  src/
    LegalDocumentAISearch.Domain          # Entities only — no dependencies
    LegalDocumentAISearch.Application     # Use-case services + all interfaces (ports)
    LegalDocumentAISearch.Infrastructure  # EF Core, OpenAI, PdfPig implementations
    LegalDocumentAISearch.Api             # Minimal API endpoints — thin HTTP layer
  tests/
    LegalDocumentAISearch.UnitTests       # References Application + Domain only
frontend/
  admin/                                  # Next.js 16 admin portal
docker-compose.yml                        # pgvector/pgvector:pg16
```

Dependency direction: `Api → Infrastructure → Application → Domain`. The test project references only `Application` and `Domain`, so unit tests never touch EF Core or OpenAI.

### Clean Architecture boundaries

**Domain** — `Document`, `DocumentChunk` entities plus `DocumentStatus` and `ChunkingStrategy` string constants.

**Application** — owns all business logic and defines every interface that infrastructure must implement:
- `Interfaces/` — `IDocumentRepository`, `ISearchRepository`, `IEmbeddingService`, `IPdfTextExtractor`, `IChunkingService`, `IIngestionQueue`, `IRagChatService`
- `Documents/` — `DocumentService` (CRUD + upload orchestration), DTOs, `UploadDocumentCommand`
- `Ingestion/` — `IngestionService` (chunk → embed → persist pipeline)
- `Search/` — `SearchService` (keyword, semantic, RAG context resolution), `RagContext`, `SearchResponse`

**Infrastructure** — implements every Application interface:
- `Repositories/` — `DocumentRepository` (EF + `ExecuteUpdateAsync`/`ExecuteDeleteAsync`), `SearchRepository` (raw SQL for tsvector and pgvector)
- `Services/` — `EmbeddingService` (batched, 100/call), `ChunkingService` (three strategies), `PdfTextExtractor`, `RagChatService` (builds prompt, streams GPT-4o)
- `Background/` — `IngestionBackgroundService` consumes `IIngestionQueue` (a `Channel<Guid>`) and calls `IIngestionService` in a scoped DI scope

**API** — minimal API endpoints, no business logic:
- `POST /api/admin/documents` — parses multipart form, builds `UploadDocumentCommand`, calls `IDocumentService`
- `GET/DELETE /api/admin/documents/{id}` — delegates directly to `IDocumentService`
- `GET /api/search/keyword` and `/semantic` — delegate to `ISearchService`, return `SearchResponse`
- `GET /api/search/rag` — calls `ISearchService.GetRagContextAsync`, then streams `IRagChatService.StreamAnswerAsync` as SSE (`text/event-stream`)

### Document ingestion pipeline

Upload (sync) → `IIngestionQueue.Enqueue(id)` → `IngestionBackgroundService` dequeues → `IngestionService.IngestAsync`:
1. Chunk via `IChunkingService` (FixedSize / ArticleLevel / Hierarchical)
2. For Hierarchical: embed only `Paragraph`-type child chunks, not article parents
3. Batch embed via `IEmbeddingService` (OpenAI `text-embedding-3-small`, 1536-dim)
4. Bulk insert via `IDocumentRepository.AddChunksAsync`
5. Set status → `Ready` (or `Failed` with error message)

Client polls `GET /api/admin/documents/{id}` to observe status transitions: `Pending → Processing → Ready | Failed`.

### Database

PostgreSQL 16 with pgvector. Key schema details:
- `Documents."TsVector"` — database-managed `tsvector` column updated by a trigger on `RawText` changes; used for keyword search via `to_tsvector('simple', ...)` (Georgian text — no stemming)
- `DocumentChunks."Embedding"` — `vector(1536)` with HNSW index (`vector_cosine_ops`); `<=>` operator = cosine distance
- Chunk hierarchy: `DocumentChunks."ParentChunkId"` self-references for Hierarchical strategy; paragraph chunks carry embeddings, article parents carry full text
- Identity tables live in the `admin` schema

### Authentication

ASP.NET Core Identity with cookie auth (`useCookies=true`). Admin endpoints use `.RequireAuthorization()`. Search endpoints are public. The frontend calls `POST /api/admin/login?useCookies=true` with `credentials: "include"`.

### Frontend note

The admin portal uses Next.js 16 which has breaking changes from earlier versions. Before modifying frontend code, read `frontend/admin/node_modules/next/dist/docs/` for current API conventions.
