using System.Text;
using LegalDocumentAISearch.Infrastructure.Services;

namespace LegalDocumentAISearch.UnitTests.Infrastructure;

public class PdfTextExtractorTests
{
    private readonly PdfTextExtractor _extractor = new();

    private static Stream ToStream(string text) =>
        new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void ExtractText_TxtFile_ReturnsRawContent()
    {
        var content = "Hello legal world";
        using var stream = ToStream(content);

        var result = _extractor.ExtractText(stream, "document.txt");

        Assert.Equal(content, result);
    }

    [Fact]
    public void ExtractText_TxtFileUpperCase_ReturnsRawContent()
    {
        var content = "Some legal text";
        using var stream = ToStream(content);

        var result = _extractor.ExtractText(stream, "DOCUMENT.TXT");

        Assert.Equal(content, result);
    }

    [Fact]
    public void ExtractText_TxtFile_PreservesMultilineContent()
    {
        var content = "Line one\nLine two\nLine three";
        using var stream = ToStream(content);

        var result = _extractor.ExtractText(stream, "file.txt");

        Assert.Equal(content, result);
    }

    [Fact]
    public void ExtractText_TxtFile_EmptyFile_ReturnsEmpty()
    {
        using var stream = ToStream(string.Empty);

        var result = _extractor.ExtractText(stream, "empty.txt");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractText_NonTxtFile_ThrowsForInvalidPdf()
    {
        using var stream = ToStream("not a real pdf");

        Assert.ThrowsAny<Exception>(() => _extractor.ExtractText(stream, "document.pdf"));
    }
}
