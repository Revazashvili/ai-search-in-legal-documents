using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalDocumentAISearch.Infrastructure.Repositories;

public class DocumentRepository(LegalDocumentsDbContext db) : IDocumentRepository
{
    public async Task<PagedResult<DocumentListItemDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Documents.OrderByDescending(d => d.UploadedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentListItemDto(
                d.Id,
                d.Title,
                d.DocumentType,
                d.ChunkingStrategy,
                d.Status,
                d.Chunks.Count,
                d.UploadedAt))
            .ToListAsync(ct);

        return new PagedResult<DocumentListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<DocumentListItemDto>> ListByStatusAsync(string status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Documents
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.UploadedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentListItemDto(
                d.Id,
                d.Title,
                d.DocumentType,
                d.ChunkingStrategy,
                d.Status,
                d.Chunks.Count,
                d.UploadedAt))
            .ToListAsync(ct);

        return new PagedResult<DocumentListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<DocumentDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default) =>
        await db.Documents
            .Where(d => d.Id == id)
            .Select(d => new DocumentDetailDto(
                d.Id,
                d.Title,
                d.SourceLawName,
                d.DocumentType,
                d.ChunkingStrategy,
                d.Status,
                d.ErrorMessage,
                d.DateEnacted,
                d.LastAmended,
                d.SourceUrl,
                d.RawText,
                d.FilePath,
                d.UploadedAt,
                d.Chunks.Count,
                d.Chunks.OrderBy(c => c.ChunkIndex).Select(c => new ChunkSummaryDto(
                    c.Id,
                    c.ChunkType,
                    c.ArticleNumber,
                    c.ChunkIndex,
                    c.ChunkText,
                    c.TokenCount,
                    c.Embedding != null)).ToList()))
            .FirstOrDefaultAsync(ct);

    public Task<Document?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Documents.FindAsync([id], ct).AsTask();

    public async Task CreateAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default)
    {
        await db.DocumentChunks.AddRangeAsync(chunks, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null, CancellationToken ct = default)
    {
        await db.Documents
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, status)
                .SetProperty(d => d.ErrorMessage, errorMessage),
                ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await db.Documents
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
