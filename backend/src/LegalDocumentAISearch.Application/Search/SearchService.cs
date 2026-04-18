using System.Diagnostics;
using LegalDocumentAISearch.Application.Interfaces;

namespace LegalDocumentAISearch.Application.Search;

public class SearchService(
    ISearchRepository searchRepository,
    IEmbeddingService embeddingService) : ISearchService
{
    public async Task<SearchResponse> KeywordSearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var results = await searchRepository.KeywordSearchAsync(query, limit, ct);
        sw.Stop();
        return new SearchResponse(query, "keyword", results, sw.ElapsedMilliseconds);
    }

    public async Task<SearchResponse> SemanticSearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var embedding = await embeddingService.GenerateEmbeddingAsync(query, ct);
        var results = await searchRepository.SemanticSearchAsync(embedding, limit, ct);
        sw.Stop();
        return new SearchResponse(query, "semantic", results, sw.ElapsedMilliseconds);
    }

    public async Task<RagContext> GetRagContextAsync(string query, CancellationToken ct = default)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(query, ct);
        var topResults = await searchRepository.SemanticSearchAsync(embedding, limit: 5, ct);

        // For hierarchical documents, swap paragraph chunk text for parent article text
        var resolvedChunks = new List<RagContextChunk>(topResults.Count);

        foreach (var result in topResults)
        {
            if (result.ParentChunkId.HasValue)
            {
                var parent = await searchRepository.GetChunkByIdAsync(result.ParentChunkId.Value, ct);
                if (parent is not null)
                {
                    resolvedChunks.Add(new RagContextChunk(
                        result.ChunkId,
                        result.DocumentId,
                        result.DocumentTitle,
                        parent.ArticleNumber,
                        parent.ChunkText,
                        result.Score));
                    continue;
                }
            }

            resolvedChunks.Add(new RagContextChunk(
                result.ChunkId,
                result.DocumentId,
                result.DocumentTitle,
                result.ArticleNumber,
                result.ChunkText,
                result.Score));
        }

        return new RagContext(query, resolvedChunks);
    }
}
