namespace LegalDocumentAISearch.Application.Search;

public interface ISearchService
{
    Task<SearchResponse> KeywordSearchAsync(string query, int limit, CancellationToken ct = default);
    Task<SearchResponse> SemanticSearchAsync(string query, int limit, CancellationToken ct = default);
    /// <summary>
    /// Retrieves and resolves the top semantic matches into a RagContext ready for LLM prompting.
    /// For hierarchical documents, paragraph chunks are automatically swapped for their parent article text.
    /// </summary>
    Task<RagContext> GetRagContextAsync(string query, CancellationToken ct = default);
}
