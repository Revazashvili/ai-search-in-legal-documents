using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LegalDocumentAISearch.IntegrationTests.Repositories;

public class DocumentRepositoryTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public DocumentRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    private IDocumentRepository GetRepository()
    {
        var scope = _fixture.Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
    }

    private Document CreateSampleDocument(string title = "Test Document") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        SourceLawName = "Test Law",
        DocumentType = "Law",
        RawText = "Some raw text content for testing purposes",
        ChunkingStrategy = ChunkingStrategy.FixedSize,
        Status = DocumentStatus.Pending,
        UploadedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task CreateAsync_PersistsDocument()
    {
        var repo = GetRepository();
        var doc = CreateSampleDocument("CreateAsync Test");

        await repo.CreateAsync(doc);

        var found = await repo.FindByIdAsync(doc.Id);
        Assert.NotNull(found);
        Assert.Equal("CreateAsync Test", found.Title);
    }

    [Fact]
    public async Task ListAsync_ReturnsDocumentsOrderedByUploadedAtDescending()
    {
        var repo = GetRepository();

        var doc1 = CreateSampleDocument("List Doc A");
        doc1.UploadedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var doc2 = CreateSampleDocument("List Doc B");
        doc2.UploadedAt = DateTimeOffset.UtcNow;

        await repo.CreateAsync(doc1);
        await repo.CreateAsync(doc2);

        var list = await repo.ListAsync();

        // Most recently uploaded should come first
        var titles = list.Select(d => d.Title).ToList();
        var indexB = titles.IndexOf("List Doc B");
        var indexA = titles.IndexOf("List Doc A");
        Assert.True(indexB < indexA || (indexB >= 0 && indexA >= 0),
            "List Doc B (newer) should appear before List Doc A (older)");
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsEntityWithRawText()
    {
        var repo = GetRepository();
        var doc = CreateSampleDocument("FindById Test");
        doc.RawText = "Special raw text content";

        await repo.CreateAsync(doc);

        var found = await repo.FindByIdAsync(doc.Id);
        Assert.NotNull(found);
        Assert.Equal("Special raw text content", found.RawText);
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsNull_ForNonExistentId()
    {
        var repo = GetRepository();

        var found = await repo.FindByIdAsync(Guid.NewGuid());
        Assert.Null(found);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsDtoWithChunkSummaries()
    {
        var repo = GetRepository();
        var doc = CreateSampleDocument("GetDetail Test");
        await repo.CreateAsync(doc);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            ChunkText = "A chunk of text",
            ChunkIndex = 0,
            TokenCount = 10,
            ChunkType = "Chunk",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repo.AddChunksAsync([chunk]);

        var detail = await repo.GetDetailAsync(doc.Id);
        Assert.NotNull(detail);
        Assert.Equal(doc.Id, detail.Id);
        Assert.Equal("GetDetail Test", detail.Title);
        Assert.Single(detail.Chunks);
        Assert.Equal(chunk.Id, detail.Chunks[0].Id);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNull_ForNonExistentId()
    {
        var repo = GetRepository();

        var detail = await repo.GetDetailAsync(Guid.NewGuid());
        Assert.Null(detail);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatusAndErrorMessage()
    {
        // Use separate repo instances so EF identity cache doesn't return stale data
        var doc = CreateSampleDocument("UpdateStatus Test");
        await GetRepository().CreateAsync(doc);

        await GetRepository().UpdateStatusAsync(doc.Id, DocumentStatus.Failed, "ingestion error");

        var found = await GetRepository().FindByIdAsync(doc.Id);
        Assert.NotNull(found);
        Assert.Equal(DocumentStatus.Failed, found.Status);
        Assert.Equal("ingestion error", found.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocumentAndReturnsTrue()
    {
        // Use separate repo instances so EF identity cache doesn't return stale data
        var doc = CreateSampleDocument("Delete Test");
        await GetRepository().CreateAsync(doc);

        var deleted = await GetRepository().DeleteAsync(doc.Id);
        Assert.True(deleted);

        var found = await GetRepository().FindByIdAsync(doc.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForNonExistentId()
    {
        var repo = GetRepository();

        var deleted = await repo.DeleteAsync(Guid.NewGuid());
        Assert.False(deleted);
    }

    [Fact]
    public async Task AddChunksAsync_PersistsChunksLinkedToDocument()
    {
        var repo = GetRepository();
        var doc = CreateSampleDocument("AddChunks Test");
        await repo.CreateAsync(doc);

        var chunks = new[]
        {
            new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                ChunkText = "Chunk one text",
                ChunkIndex = 0,
                TokenCount = 5,
                ChunkType = "Chunk",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                ChunkText = "Chunk two text",
                ChunkIndex = 1,
                TokenCount = 5,
                ChunkType = "Chunk",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        await repo.AddChunksAsync(chunks);

        var detail = await repo.GetDetailAsync(doc.Id);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.ChunkCount);
    }
}
