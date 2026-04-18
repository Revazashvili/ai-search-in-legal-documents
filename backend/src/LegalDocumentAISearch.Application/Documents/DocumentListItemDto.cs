namespace LegalDocumentAISearch.Application.Documents;

public record DocumentListItemDto(
    Guid Id,
    string Title,
    string DocumentType,
    string ChunkingStrategy,
    string Status,
    int ChunkCount,
    DateTimeOffset UploadedAt);
