using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using NSubstitute.ExceptionExtensions;

namespace LegalDocumentAISearch.UnitTests.Application;

public class DocumentServiceTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IIngestionQueue _ingestionQueue;
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _pdfTextExtractor = Substitute.For<IPdfTextExtractor>();
        _ingestionQueue = Substitute.For<IIngestionQueue>();
        _sut = new DocumentService(_documentRepository, _pdfTextExtractor, _ingestionQueue);
    }

    private static UploadDocumentCommand BuildCommand(Stream? stream = null) =>
        new(
            stream ?? new MemoryStream(),
            "test.pdf",
            "Test Title",
            "Test Law",
            "Contract",
            ChunkingStrategy.FixedSize,
            null,
            null,
            null);

    [Fact]
    public async Task UploadDocumentAsync_WhenExtractionThrows_ReturnsFailure()
    {
        _pdfTextExtractor
            .ExtractText(Arg.Any<Stream>(), Arg.Any<string>())
            .Throws(new InvalidOperationException("bad pdf"));

        var result = await _sut.UploadDocumentAsync(BuildCommand());

        Assert.False(result.IsSuccess);
        Assert.Contains("bad pdf", result.Error);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenExtractedTextIsEmpty_ReturnsFailure()
    {
        _pdfTextExtractor
            .ExtractText(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns(string.Empty);

        var result = await _sut.UploadDocumentAsync(BuildCommand());

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenSuccess_CreatesDocumentWithCorrectMetadata()
    {
        _pdfTextExtractor
            .ExtractText(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns("Some legal text content.");
        _documentRepository.CreateAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var command = BuildCommand();
        var result = await _sut.UploadDocumentAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test Title", result.Title);
        await _documentRepository.Received(1).CreateAsync(
            Arg.Is<Document>(d =>
                d.Title == command.Title &&
                d.SourceLawName == command.SourceLawName &&
                d.DocumentType == command.DocumentType &&
                d.ChunkingStrategy == command.ChunkingStrategy &&
                d.RawText == "Some legal text content." &&
                d.Status == DocumentStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenSuccess_EnqueuesDocumentId()
    {
        _pdfTextExtractor
            .ExtractText(Arg.Any<Stream>(), Arg.Any<string>())
            .Returns("Some legal text content.");
        _documentRepository.CreateAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.UploadDocumentAsync(BuildCommand());

        Assert.True(result.IsSuccess);
        _ingestionQueue.Received(1).Enqueue(result.DocumentId!.Value);
    }

    [Fact]
    public async Task ListDocumentsAsync_DelegatesToRepository()
    {
        var expected = new List<DocumentListItemDto>
        {
            new(Guid.NewGuid(), "Doc1", "Contract", "FixedSize", "Ready", 5, DateTimeOffset.UtcNow)
        };
        _documentRepository.ListAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.ListDocumentsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetDocumentAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        var expected = new DocumentDetailDto(id, "Doc", "Law", "Contract", "FixedSize", "Ready", null, null, null, null, DateTimeOffset.UtcNow, 0, []);
        _documentRepository.GetDetailAsync(id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetDocumentAsync(id);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task DeleteDocumentAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        _documentRepository.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteDocumentAsync(id);

        Assert.True(result);
        await _documentRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
