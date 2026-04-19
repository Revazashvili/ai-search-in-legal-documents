using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.Infrastructure.Services;

namespace LegalDocumentAISearch.UnitTests.Infrastructure;

public class ChunkingServiceTests
{
    private readonly ChunkingService _sut = new();
    private static readonly Guid DocId = Guid.NewGuid();

    // --- FixedSize ---

    [Fact]
    public void FixedSize_ShortText_ProducesOneChunk()
    {
        var chunks = _sut.Chunk(DocId, "Hello world", ChunkingStrategy.FixedSize);
        Assert.Single(chunks);
    }

    [Fact]
    public void FixedSize_TextLongerThan500Tokens_ProducesMultipleChunks()
    {
        // Generate text that exceeds the 500-token window
        var longText = string.Join(" ", Enumerable.Repeat("word", 600));
        var chunks = _sut.Chunk(DocId, longText, ChunkingStrategy.FixedSize);
        Assert.True(chunks.Count > 1);
    }

    [Fact]
    public void FixedSize_AllChunks_HaveChunkTypeChunk()
    {
        var chunks = _sut.Chunk(DocId, "Some text content for testing chunking.", ChunkingStrategy.FixedSize);
        Assert.All(chunks, c => Assert.Equal("Chunk", c.ChunkType));
    }

    [Fact]
    public void FixedSize_Chunks_HaveSequentialChunkIndex()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 600));
        var chunks = _sut.Chunk(DocId, longText, ChunkingStrategy.FixedSize);
        for (int i = 0; i < chunks.Count; i++)
            Assert.Equal(i, chunks[i].ChunkIndex);
    }

    // --- ArticleLevel ---

    [Fact]
    public void ArticleLevel_TextWithEnglishArticle_SplitsAtArticleBoundary()
    {
        var text = "Preamble text.\n\nArticle 1\nFirst article content.\n\nArticle 2\nSecond article content.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.ArticleLevel);
        Assert.True(chunks.Count >= 2);
        Assert.Contains(chunks, c => c.ArticleNumber == "1");
        Assert.Contains(chunks, c => c.ArticleNumber == "2");
    }

    [Fact]
    public void ArticleLevel_TextWithGeorgianArticle_SplitsAtGeorgianArticle()
    {
        var text = "მუხლი 1\nპირველი მუხლის შინაარსი.\n\nმუხლი 2\nმეორე მუხლის შინაარსი.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.ArticleLevel);
        Assert.True(chunks.Count >= 2);
        Assert.Contains(chunks, c => c.ArticleNumber == "1");
        Assert.Contains(chunks, c => c.ArticleNumber == "2");
    }

    [Fact]
    public void ArticleLevel_TextWithNoArticles_ProducesSingleChunk()
    {
        var text = "Just some plain text with no article headers.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.ArticleLevel);
        Assert.Single(chunks);
    }

    [Fact]
    public void ArticleLevel_PreambleBeforeFirstArticle_IsIncludedAsSeparateChunk()
    {
        var text = "This is a preamble section.\n\nArticle 1\nFirst article content.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.ArticleLevel);
        Assert.True(chunks.Count >= 2);
        // Preamble chunk has no article number
        var preamble = chunks.FirstOrDefault(c => c.ArticleNumber == null);
        Assert.NotNull(preamble);
        Assert.Contains("preamble", preamble.ChunkText, StringComparison.OrdinalIgnoreCase);
    }

    // --- Hierarchical ---

    [Fact]
    public void Hierarchical_CreatesArticleParentAndParagraphChildren()
    {
        var text = "Article 1\nFirst paragraph.\n\nSecond paragraph.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.Hierarchical);
        Assert.Contains(chunks, c => c.ChunkType == "Article");
        Assert.Contains(chunks, c => c.ChunkType == "Paragraph");
    }

    [Fact]
    public void Hierarchical_ChildChunks_HaveParentChunkId()
    {
        var text = "Article 1\nFirst paragraph.\n\nSecond paragraph.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.Hierarchical);
        var parent = chunks.First(c => c.ChunkType == "Article");
        var children = chunks.Where(c => c.ChunkType == "Paragraph").ToList();
        Assert.NotEmpty(children);
        Assert.All(children, c => Assert.Equal(parent.Id, c.ParentChunkId));
    }

    [Fact]
    public void Hierarchical_ChildChunkType_IsParagraph_ParentChunkType_IsArticle()
    {
        var text = "Article 1\nContent here.\n\nMore content.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.Hierarchical);
        Assert.All(chunks.Where(c => c.ParentChunkId.HasValue), c => Assert.Equal("Paragraph", c.ChunkType));
        Assert.All(chunks.Where(c => !c.ParentChunkId.HasValue), c => Assert.Equal("Article", c.ChunkType));
    }

    [Fact]
    public void Hierarchical_NoParagraphsInBody_CreatesSingleParagraphChild()
    {
        // No double newlines in body — single paragraph content
        var text = "Article 1\nSingle line content with no double newline.";
        var chunks = _sut.Chunk(DocId, text, ChunkingStrategy.Hierarchical);
        var parent = chunks.Single(c => c.ChunkType == "Article");
        var children = chunks.Where(c => c.ChunkType == "Paragraph").ToList();
        Assert.Single(children);
        Assert.Equal(parent.Id, children[0].ParentChunkId);
    }
}
