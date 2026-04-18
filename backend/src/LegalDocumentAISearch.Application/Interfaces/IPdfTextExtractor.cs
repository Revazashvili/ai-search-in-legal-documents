namespace LegalDocumentAISearch.Application.Interfaces;

public interface IPdfTextExtractor
{
    string ExtractText(Stream stream, string fileName);
}
