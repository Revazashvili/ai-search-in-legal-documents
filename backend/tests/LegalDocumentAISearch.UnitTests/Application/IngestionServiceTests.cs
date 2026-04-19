using LegalDocumentAISearch.Application.Ingestion;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace LegalDocumentAISearch.UnitTests.Application;

public class IngestionServiceTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IngestionService _sut;

    public IngestionServiceTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _chunkingService = Substitute.For<IChunkingService>();
        _embeddingService = Substitute.For<IEmbeddingService>();
        _sut = new IngestionService(
            _documentRepository,
            _chunkingService,
            _embeddingService,
            NullLogger<IngestionService>.Instance);
    }

    private void SetupSuccessfulPipeline(Guid id, Document document, List<DocumentChunk> chunks, float[][] embeddings)
    {
        _documentRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns(document);
        _chunkingService.Chunk(id, Arg.Any<string>(), Arg.Any<string>()).Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(embeddings);
        _documentRepository.AddChunksAsync(Arg.Any<IEnumerable<DocumentChunk>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _documentRepository.UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task IngestAsync_WhenDocumentNotFound_ReturnsEarlyWithNoStatusUpdate()
    {
        var id = Guid.NewGuid();
        _documentRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Document?)null);

        await _sut.IngestAsync(id);

        await _documentRepository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_WhenDocumentFound_UpdatesStatusToProcessing()
    {
        var id = Guid.NewGuid();
        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.FixedSize };
        SetupSuccessfulPipeline(id, document, [], Array.Empty<float[]>());

        await _sut.IngestAsync(id);

        await _documentRepository.Received().UpdateStatusAsync(
            id, DocumentStatus.Processing, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_OnSuccessfulIngestion_UpdatesStatusToReady()
    {
        var id = Guid.NewGuid();
        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.FixedSize };
        SetupSuccessfulPipeline(id, document, [], Array.Empty<float[]>());

        await _sut.IngestAsync(id);

        await _documentRepository.Received().UpdateStatusAsync(
            id, DocumentStatus.Ready, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_OnSuccessfulIngestion_CallsAddChunksAsync()
    {
        var id = Guid.NewGuid();
        var chunks = new List<DocumentChunk>
        {
            new() { Id = Guid.NewGuid(), DocumentId = id, ChunkText = "chunk1" }
        };
        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.FixedSize };
        SetupSuccessfulPipeline(id, document, chunks, [new float[] { 0.1f, 0.2f }]);

        await _sut.IngestAsync(id);

        await _documentRepository.Received(1).AddChunksAsync(chunks, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_WithHierarchicalStrategy_OnlyParagraphChunksGetEmbeddings()
    {
        var id = Guid.NewGuid();
        var articleChunk = new DocumentChunk
        {
            Id = Guid.NewGuid(), DocumentId = id, ChunkType = "Article", ChunkText = "Article 1\nfull text"
        };
        var paragraphChunk = new DocumentChunk
        {
            Id = Guid.NewGuid(), DocumentId = id, ChunkType = "Paragraph",
            ChunkText = "paragraph text", ParentChunkId = articleChunk.Id
        };
        var chunks = new List<DocumentChunk> { articleChunk, paragraphChunk };

        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.Hierarchical };
        SetupSuccessfulPipeline(id, document, chunks, [new float[] { 0.1f }]);

        await _sut.IngestAsync(id);

        // Only paragraph chunk should have its embedding set
        Assert.NotNull(paragraphChunk.Embedding);
        Assert.Null(articleChunk.Embedding);
        await _embeddingService.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == paragraphChunk.ChunkText),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_WithNonHierarchicalStrategy_AllChunksGetEmbeddings()
    {
        var id = Guid.NewGuid();
        var chunk1 = new DocumentChunk { Id = Guid.NewGuid(), DocumentId = id, ChunkType = "Chunk", ChunkText = "chunk 1" };
        var chunk2 = new DocumentChunk { Id = Guid.NewGuid(), DocumentId = id, ChunkType = "Chunk", ChunkText = "chunk 2" };
        var chunks = new List<DocumentChunk> { chunk1, chunk2 };

        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.FixedSize };
        SetupSuccessfulPipeline(id, document, chunks, [new float[] { 0.1f }, new float[] { 0.2f }]);

        await _sut.IngestAsync(id);

        await _embeddingService.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 2),
            Arg.Any<CancellationToken>());
        Assert.NotNull(chunk1.Embedding);
        Assert.NotNull(chunk2.Embedding);
    }

    [Fact]
    public async Task IngestAsync_WhenExceptionOccurs_UpdatesStatusToFailed()
    {
        var id = Guid.NewGuid();
        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.FixedSize };
        _documentRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns(document);
        _chunkingService.Chunk(id, Arg.Any<string>(), Arg.Any<string>())
            .Throws(new InvalidOperationException("chunking failed"));
        _documentRepository.UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _sut.IngestAsync(id);

        await _documentRepository.Received(1).UpdateStatusAsync(
            id,
            DocumentStatus.Failed,
            Arg.Is<string?>(msg => msg != null && msg.Contains("chunking failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_WhenExceptionOccurs_UpdatesStatusWithCancellationTokenNone()
    {
        var id = Guid.NewGuid();
        var document = new Document { Id = id, RawText = "text", ChunkingStrategy = ChunkingStrategy.FixedSize };
        _documentRepository.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns(document);
        _chunkingService.Chunk(id, Arg.Any<string>(), Arg.Any<string>())
            .Throws(new InvalidOperationException("chunking failed"));
        _documentRepository.UpdateStatusAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _sut.IngestAsync(id);

        await _documentRepository.Received(1).UpdateStatusAsync(
            id,
            DocumentStatus.Failed,
            Arg.Any<string?>(),
            CancellationToken.None);
    }
}
