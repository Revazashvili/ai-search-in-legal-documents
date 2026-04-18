namespace LegalDocumentAISearch.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? ParentChunkId { get; set; }
    public string ChunkType { get; set; } = "Chunk";
    public string? ArticleNumber { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public float[]? Embedding { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Document Document { get; set; } = null!;
    public DocumentChunk? ParentChunk { get; set; }
    public ICollection<DocumentChunk> ChildChunks { get; set; } = [];
}
