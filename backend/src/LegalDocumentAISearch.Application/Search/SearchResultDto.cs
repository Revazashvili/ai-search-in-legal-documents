namespace LegalDocumentAISearch.Application.Search;

public record SearchResultDto(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string? ArticleNumber,
    string ChunkText,
    double Score,
    Guid? ParentChunkId = null);
