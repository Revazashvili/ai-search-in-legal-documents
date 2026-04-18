namespace LegalDocumentAISearch.Application.Search;

/// <summary>
/// The resolved context chunks passed to the LLM for RAG generation.
/// Each chunk's Text already reflects parent article text for hierarchical docs.
/// </summary>
public record RagContext(string Query, IReadOnlyList<RagContextChunk> Chunks);

public record RagContextChunk(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string? ArticleNumber,
    string Text,
    double Score);
