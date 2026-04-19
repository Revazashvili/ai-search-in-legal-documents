using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.UnitTests.Domain;

public class DocumentChunkTests
{
    [Fact]
    public void DocumentChunk_DefaultChunkType_IsChunk()
    {
        var chunk = new DocumentChunk();
        Assert.Equal("Chunk", chunk.ChunkType);
    }

    [Fact]
    public void DocumentChunk_DefaultChildChunks_IsEmpty()
    {
        var chunk = new DocumentChunk();
        Assert.Empty(chunk.ChildChunks);
    }

    [Fact]
    public void DocumentChunk_DefaultEmbedding_IsNull()
    {
        var chunk = new DocumentChunk();
        Assert.Null(chunk.Embedding);
    }

    [Fact]
    public void DocumentChunk_DefaultParentChunkId_IsNull()
    {
        var chunk = new DocumentChunk();
        Assert.Null(chunk.ParentChunkId);
    }

    [Fact]
    public void DocumentChunk_SettingParentChunkId_Works()
    {
        var parentId = Guid.NewGuid();
        var chunk = new DocumentChunk { ParentChunkId = parentId };
        Assert.Equal(parentId, chunk.ParentChunkId);
    }
}
