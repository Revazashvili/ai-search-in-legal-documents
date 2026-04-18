namespace LegalDocumentAISearch.Application.Documents;

public record DocumentDetailDto(
    Guid Id,
    string Title,
    string SourceLawName,
    string DocumentType,
    string ChunkingStrategy,
    string Status,
    string? ErrorMessage,
    DateOnly? DateEnacted,
    DateOnly? LastAmended,
    string? SourceUrl,
    DateTimeOffset UploadedAt,
    int ChunkCount,
    IReadOnlyList<ChunkSummaryDto> Chunks);

public record ChunkSummaryDto(
    Guid Id,
    string ChunkType,
    string? ArticleNumber,
    int ChunkIndex,
    int TokenCount,
    bool HasEmbedding);
