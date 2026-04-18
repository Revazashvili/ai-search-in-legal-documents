namespace LegalDocumentAISearch.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceLawName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateOnly? DateEnacted { get; set; }
    public DateOnly? LastAmended { get; set; }
    public string? SourceUrl { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string ChunkingStrategy { get; set; } = string.Empty;
    public string Status { get; set; } = DocumentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}

public static class DocumentStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Ready = "Ready";
    public const string Failed = "Failed";
}

public static class ChunkingStrategy
{
    public const string FixedSize = "FixedSize";
    public const string ArticleLevel = "ArticleLevel";
    public const string Hierarchical = "Hierarchical";
}
