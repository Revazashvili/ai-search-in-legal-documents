using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Application.Search;
using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.UnitTests.Application;

public class SearchServiceTests
{
    private readonly ISearchRepository _searchRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly SearchService _sut;

    public SearchServiceTests()
    {
        _searchRepository = Substitute.For<ISearchRepository>();
        _embeddingService = Substitute.For<IEmbeddingService>();
        _sut = new SearchService(_searchRepository, _embeddingService);
    }

    [Fact]
    public async Task KeywordSearchAsync_DelegatesToRepositoryAndReturnsModeKeyword()
    {
        var results = new List<SearchResultDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Doc1", null, "text", 0.9)
        };
        _searchRepository.KeywordSearchAsync("query", 10, Arg.Any<CancellationToken>())
            .Returns(results);

        var response = await _sut.KeywordSearchAsync("query", 10);

        Assert.Equal("keyword", response.Mode);
        Assert.Equal("query", response.Query);
        Assert.Equal(results, response.Results);
    }

    [Fact]
    public async Task SemanticSearchAsync_GeneratesEmbeddingThenSearchesAndReturnsModeSemanticl()
    {
        var embedding = new float[] { 0.1f, 0.2f };
        var results = new List<SearchResultDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Doc1", null, "text", 0.95)
        };
        _embeddingService.GenerateEmbeddingAsync("query", Arg.Any<CancellationToken>())
            .Returns(embedding);
        _searchRepository.SemanticSearchAsync(embedding, 5, Arg.Any<CancellationToken>())
            .Returns(results);

        var response = await _sut.SemanticSearchAsync("query", 5);

        Assert.Equal("semantic", response.Mode);
        Assert.Equal("query", response.Query);
        Assert.Equal(results, response.Results);
        await _embeddingService.Received(1).GenerateEmbeddingAsync("query", Arg.Any<CancellationToken>());
        await _searchRepository.Received(1).SemanticSearchAsync(embedding, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRagContextAsync_CallsSemanticSearchWithLimit5()
    {
        var embedding = new float[] { 0.1f };
        _embeddingService.GenerateEmbeddingAsync("query", Arg.Any<CancellationToken>())
            .Returns(embedding);
        _searchRepository.SemanticSearchAsync(embedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<SearchResultDto>());

        await _sut.GetRagContextAsync("query");

        await _searchRepository.Received(1).SemanticSearchAsync(embedding, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRagContextAsync_WhenResultHasParentChunkId_FetchesParentAndUsesParentText()
    {
        var parentId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var embedding = new float[] { 0.1f };

        var result = new SearchResultDto(chunkId, docId, "Doc1", "2", "paragraph text", 0.9, parentId);
        var parentChunk = new DocumentChunk
        {
            Id = parentId, ChunkText = "full article text", ArticleNumber = "1"
        };

        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(embedding);
        _searchRepository.SemanticSearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchResultDto> { result });
        _searchRepository.GetChunkByIdAsync(parentId, Arg.Any<CancellationToken>())
            .Returns(parentChunk);

        var ragContext = await _sut.GetRagContextAsync("query");

        Assert.Single(ragContext.Chunks);
        var chunk = ragContext.Chunks[0];
        Assert.Equal("full article text", chunk.Text);
        Assert.Equal("1", chunk.ArticleNumber);
        Assert.Equal(chunkId, chunk.ChunkId);
        Assert.Equal(docId, chunk.DocumentId);
    }

    [Fact]
    public async Task GetRagContextAsync_WhenResultHasNoParentChunkId_UsesOriginalChunkText()
    {
        var chunkId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var embedding = new float[] { 0.1f };

        var result = new SearchResultDto(chunkId, docId, "Doc1", "3", "original text", 0.85, null);

        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(embedding);
        _searchRepository.SemanticSearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchResultDto> { result });

        var ragContext = await _sut.GetRagContextAsync("query");

        Assert.Single(ragContext.Chunks);
        Assert.Equal("original text", ragContext.Chunks[0].Text);
        Assert.Equal("3", ragContext.Chunks[0].ArticleNumber);
    }

    [Fact]
    public async Task GetRagContextAsync_WhenParentNotFound_FallsBackToOriginalChunkText()
    {
        var parentId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var embedding = new float[] { 0.1f };

        var result = new SearchResultDto(chunkId, docId, "Doc1", "5", "original text", 0.88, parentId);

        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(embedding);
        _searchRepository.SemanticSearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SearchResultDto> { result });
        _searchRepository.GetChunkByIdAsync(parentId, Arg.Any<CancellationToken>())
            .Returns((DocumentChunk?)null);

        var ragContext = await _sut.GetRagContextAsync("query");

        Assert.Single(ragContext.Chunks);
        Assert.Equal("original text", ragContext.Chunks[0].Text);
        Assert.Equal("5", ragContext.Chunks[0].ArticleNumber);
    }
}
