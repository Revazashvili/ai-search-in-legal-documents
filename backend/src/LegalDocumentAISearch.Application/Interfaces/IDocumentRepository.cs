using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Application.Interfaces;

public interface IDocumentRepository
{
    Task<List<DocumentListItemDto>> ListAsync(CancellationToken ct = default);
    Task<DocumentDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default);
    /// <summary>Returns the full entity including RawText — used by the ingestion pipeline.</summary>
    Task<Document?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateAsync(Document document, CancellationToken ct = default);
    Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, string status, string? errorMessage = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
