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
    string RawText,
    string? FilePath,
    DateTimeOffset UploadedAt,
    int ChunkCount,
    IReadOnlyList<ChunkSummaryDto> Chunks);

public record ChunkSummaryDto(
    Guid Id,
    string ChunkType,
    string? ArticleNumber,
    int ChunkIndex,
    string ChunkText,
    int TokenCount,
    bool HasEmbedding);
