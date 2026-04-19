using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LegalDocumentAISearch.IntegrationTests.Repositories;

public class SearchRepositoryTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public SearchRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IDocumentRepository GetDocumentRepository()
    {
        var scope = _fixture.Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
    }

    private ISearchRepository GetSearchRepository()
    {
        var scope = _fixture.Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISearchRepository>();
    }

    private async Task<(Document doc, DocumentChunk chunk)> SeedReadyDocumentWithChunk(
        string chunkText,
        float[]? embedding = null,
        string? articleNumber = null)
    {
        var docRepo = GetDocumentRepository();

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = "Search Test Document",
            SourceLawName = "Search Law",
            DocumentType = "Law",
            RawText = chunkText,
            ChunkingStrategy = ChunkingStrategy.FixedSize,
            Status = DocumentStatus.Ready,
            UploadedAt = DateTimeOffset.UtcNow
        };
        await docRepo.CreateAsync(doc);

        // Update status to Ready
        await docRepo.UpdateStatusAsync(doc.Id, DocumentStatus.Ready);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            ChunkText = chunkText,
            ChunkIndex = 0,
            TokenCount = 10,
            ChunkType = "Chunk",
            ArticleNumber = articleNumber,
            Embedding = embedding,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await docRepo.AddChunksAsync([chunk]);

        return (doc, chunk);
    }

    [Fact]
    public async Task KeywordSearchAsync_ReturnsResultsMatchingQueryTerm()
    {
        var uniqueWord = "xyzuniquekeyword" + Guid.NewGuid().ToString("N")[..8];
        var (_, _) = await SeedReadyDocumentWithChunk($"This text contains the {uniqueWord} term for testing");

        var searchRepo = GetSearchRepository();
        var results = await searchRepo.KeywordSearchAsync(uniqueWord, 10);

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Contains(uniqueWord, r.ChunkText, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task KeywordSearchAsync_ReturnsEmpty_WhenNoMatch()
    {
        var searchRepo = GetSearchRepository();
        var results = await searchRepo.KeywordSearchAsync("xyznosuchtermexists99999", 10);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SemanticSearchAsync_ReturnsSortedByCosineSimilarity()
    {
        // Seed a chunk with a known embedding (all 0.5f at dim 768)
        var embedding = new float[768];
        Array.Fill(embedding, 0.5f);

        var (_, seededChunk) = await SeedReadyDocumentWithChunk(
            "Semantic search test content",
            embedding,
            articleNumber: "5");

        // Query with the same embedding — should be the best match (distance = 0)
        var searchRepo = GetSearchRepository();
        var results = await searchRepo.SemanticSearchAsync(embedding, 10);

        Assert.NotEmpty(results);
        // The seeded chunk should appear in results
        Assert.Contains(results, r => r.ChunkId == seededChunk.Id);
    }

    [Fact]
    public async Task SemanticSearchAsync_ExcludesChunksWithNullEmbedding()
    {
        // Seed a chunk WITHOUT an embedding
        var (_, chunkNoEmbedding) = await SeedReadyDocumentWithChunk(
            "No embedding chunk content");

        var queryEmbedding = new float[768];
        Array.Fill(queryEmbedding, 0.1f);

        var searchRepo = GetSearchRepository();
        var results = await searchRepo.SemanticSearchAsync(queryEmbedding, 50);

        Assert.DoesNotContain(results, r => r.ChunkId == chunkNoEmbedding.Id);
    }

    [Fact]
    public async Task GetChunkByIdAsync_ReturnsCorrectChunk()
    {
        var (_, chunk) = await SeedReadyDocumentWithChunk("GetChunkById test content");

        var searchRepo = GetSearchRepository();
        var found = await searchRepo.GetChunkByIdAsync(chunk.Id);

        Assert.NotNull(found);
        Assert.Equal(chunk.Id, found.Id);
        Assert.Equal(chunk.ChunkText, found.ChunkText);
    }

    [Fact]
    public async Task GetChunkByIdAsync_ReturnsNull_ForNonExistentId()
    {
        var searchRepo = GetSearchRepository();
        var found = await searchRepo.GetChunkByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }
}
