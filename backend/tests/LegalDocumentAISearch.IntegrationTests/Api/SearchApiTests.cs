using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Application.Search;
using LegalDocumentAISearch.IntegrationTests.Fixtures;
using NSubstitute;

namespace LegalDocumentAISearch.IntegrationTests.Api;

public class SearchApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public SearchApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        // Set up default mock behaviour for EmbeddingService
        fixture.EmbeddingService
            .GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new float[768]));
    }

    private HttpClient CreateClient() => _fixture.Factory.CreateClient();

    [Fact]
    public async Task KeywordSearch_Returns200_WithSearchResponse()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/search/keyword?q=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("query", out _) ||
                    doc.RootElement.TryGetProperty("Query", out _),
            "Response should contain 'query' or 'Query' property");
    }

    [Fact]
    public async Task KeywordSearch_Returns400_WhenNoQuery()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/search/keyword");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SemanticSearch_Returns200_WithSearchResponse()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/search/semantic?q=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("query", out _) ||
                    doc.RootElement.TryGetProperty("Query", out _),
            "Response should contain 'query' or 'Query' property");
    }

    [Fact]
    public async Task SemanticSearch_Returns400_WhenNoQuery()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/search/semantic");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RagSearch_Returns200_WithSseContentType()
    {
        var client = CreateClient();

        // Set up mock to return an empty async enumerable (no chunks → SSE done event)
        _fixture.RagChatService
            .StreamAnswerAsync(Arg.Any<string>(), Arg.Any<RagContext>(), Arg.Any<CancellationToken>())
            .Returns(EmptyAsyncEnumerable());

        var response = await client.GetAsync("/api/search/rag?q=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task RagSearch_Returns400_WhenNoQuery()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/search/rag");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

#pragma warning disable CS1998
    private static async IAsyncEnumerable<string> EmptyAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield break;
    }
#pragma warning restore CS1998
}
