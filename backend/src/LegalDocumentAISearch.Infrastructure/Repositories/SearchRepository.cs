using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Application.Search;
using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalDocumentAISearch.Infrastructure.Repositories;

public class SearchRepository(LegalDocumentsDbContext db) : ISearchRepository
{
    public Task<List<SearchResultDto>> KeywordSearchAsync(string query, int limit, CancellationToken ct = default) =>
        db.Database.SqlQuery<SearchResultDto>(
            $"""
            SELECT c."Id" AS "ChunkId",
                   c."DocumentId",
                   d."Title" AS "DocumentTitle",
                   c."ArticleNumber",
                   c."ChunkText",
                   CAST(ts_rank(to_tsvector('simple', c."ChunkText"), plainto_tsquery('simple', {query})) AS double precision) AS "Score",
                   c."ParentChunkId"
            FROM "DocumentChunks" c
            JOIN "Documents" d ON c."DocumentId" = d."Id"
            WHERE d."Status" = 'Ready'
              AND to_tsvector('simple', c."ChunkText") @@ plainto_tsquery('simple', {query})
            ORDER BY "Score" DESC
            LIMIT {limit}
            """)
        .ToListAsync(ct);

    public Task<List<SearchResultDto>> SemanticSearchAsync(float[] queryEmbedding, int limit, CancellationToken ct = default)
    {
        var vectorLiteral = $"[{string.Join(",", queryEmbedding)}]";
        var sql = $"""
            SELECT c."Id" AS "ChunkId",
                   c."DocumentId",
                   d."Title" AS "DocumentTitle",
                   c."ArticleNumber",
                   c."ChunkText",
                   CAST(1 - (c."Embedding" <=> '{vectorLiteral}'::vector) AS double precision) AS "Score",
                   c."ParentChunkId"
            FROM "DocumentChunks" c
            JOIN "Documents" d ON c."DocumentId" = d."Id"
            WHERE d."Status" = 'Ready'
              AND c."Embedding" IS NOT NULL
            ORDER BY c."Embedding" <=> '{vectorLiteral}'::vector
            LIMIT {limit}
            """;

        return db.Database.SqlQueryRaw<SearchResultDto>(sql).ToListAsync(ct);
    }

    public Task<DocumentChunk?> GetChunkByIdAsync(Guid id, CancellationToken ct = default) =>
        db.DocumentChunks.FindAsync([id], ct).AsTask();
}
