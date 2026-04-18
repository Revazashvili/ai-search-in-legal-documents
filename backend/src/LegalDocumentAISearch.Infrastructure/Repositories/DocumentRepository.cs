using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalDocumentAISearch.Infrastructure.Repositories;

public class DocumentRepository(LegalDocumentsDbContext db) : IDocumentRepository
{
    public Task<List<DocumentListItemDto>> ListAsync(CancellationToken ct = default) =>
        db.Documents
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentListItemDto(
                d.Id,
                d.Title,
                d.DocumentType,
                d.ChunkingStrategy,
                d.Status,
                d.Chunks.Count,
                d.UploadedAt))
            .ToListAsync(ct);

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
                d.UploadedAt,
                d.Chunks.Count,
                d.Chunks.OrderBy(c => c.ChunkIndex).Select(c => new ChunkSummaryDto(
                    c.Id,
                    c.ChunkType,
                    c.ArticleNumber,
                    c.ChunkIndex,
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
