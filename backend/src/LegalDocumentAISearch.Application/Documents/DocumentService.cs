using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Application.Documents;

public class DocumentService(
    IDocumentRepository documentRepository,
    IPdfTextExtractor pdfTextExtractor,
    IIngestionQueue ingestionQueue) : IDocumentService
{
    public Task<List<DocumentListItemDto>> ListDocumentsAsync(CancellationToken ct = default) =>
        documentRepository.ListAsync(ct);

    public Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken ct = default) =>
        documentRepository.GetDetailAsync(id, ct);

    public async Task<UploadDocumentResult> UploadDocumentAsync(UploadDocumentCommand command, CancellationToken ct = default)
    {
        string rawText;
        try
        {
            rawText = pdfTextExtractor.ExtractText(command.FileStream, command.FileName);
        }
        catch (Exception ex)
        {
            return UploadDocumentResult.Failure($"Failed to extract text from file: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(rawText))
            return UploadDocumentResult.Failure("Could not extract text from the uploaded file.");

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            SourceLawName = command.SourceLawName,
            DocumentType = command.DocumentType,
            ChunkingStrategy = command.ChunkingStrategy,
            DateEnacted = command.DateEnacted,
            LastAmended = command.LastAmended,
            SourceUrl = command.SourceUrl,
            RawText = rawText,
            Status = DocumentStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow
        };

        await documentRepository.CreateAsync(document, ct);
        ingestionQueue.Enqueue(document.Id);

        return UploadDocumentResult.Success(document.Id, document.Title);
    }

    public Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default) =>
        documentRepository.DeleteAsync(id, ct);
}
