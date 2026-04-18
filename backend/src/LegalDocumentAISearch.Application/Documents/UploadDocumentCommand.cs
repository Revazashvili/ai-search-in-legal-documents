namespace LegalDocumentAISearch.Application.Documents;

public record UploadDocumentCommand(
    Stream FileStream,
    string FileName,
    string Title,
    string SourceLawName,
    string DocumentType,
    string ChunkingStrategy,
    DateOnly? DateEnacted,
    DateOnly? LastAmended,
    string? SourceUrl);
