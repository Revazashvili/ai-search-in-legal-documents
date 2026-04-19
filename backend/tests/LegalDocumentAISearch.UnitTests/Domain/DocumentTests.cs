using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.UnitTests.Domain;

public class DocumentTests
{
    [Fact]
    public void Document_DefaultStatus_IsPending()
    {
        var doc = new Document();
        Assert.Equal("Pending", doc.Status);
    }

    [Fact]
    public void Document_DefaultChunks_IsEmpty()
    {
        var doc = new Document();
        Assert.Empty(doc.Chunks);
    }

    [Fact]
    public void DocumentStatus_Pending_EqualsExpectedString()
    {
        Assert.Equal("Pending", DocumentStatus.Pending);
    }

    [Fact]
    public void DocumentStatus_Processing_EqualsExpectedString()
    {
        Assert.Equal("Processing", DocumentStatus.Processing);
    }

    [Fact]
    public void DocumentStatus_Ready_EqualsExpectedString()
    {
        Assert.Equal("Ready", DocumentStatus.Ready);
    }

    [Fact]
    public void DocumentStatus_Failed_EqualsExpectedString()
    {
        Assert.Equal("Failed", DocumentStatus.Failed);
    }

    [Fact]
    public void ChunkingStrategy_FixedSize_EqualsExpectedString()
    {
        Assert.Equal("FixedSize", ChunkingStrategy.FixedSize);
    }

    [Fact]
    public void ChunkingStrategy_ArticleLevel_EqualsExpectedString()
    {
        Assert.Equal("ArticleLevel", ChunkingStrategy.ArticleLevel);
    }

    [Fact]
    public void ChunkingStrategy_Hierarchical_EqualsExpectedString()
    {
        Assert.Equal("Hierarchical", ChunkingStrategy.Hierarchical);
    }
}
