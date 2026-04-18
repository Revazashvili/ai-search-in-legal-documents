using LegalDocumentAISearch.Application.Search;
using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Application.Interfaces;

public interface ISearchRepository
{
    Task<List<SearchResultDto>> KeywordSearchAsync(string query, int limit, CancellationToken ct = default);
    Task<List<SearchResultDto>> SemanticSearchAsync(float[] queryEmbedding, int limit, CancellationToken ct = default);
    Task<DocumentChunk?> GetChunkByIdAsync(Guid id, CancellationToken ct = default);
}
