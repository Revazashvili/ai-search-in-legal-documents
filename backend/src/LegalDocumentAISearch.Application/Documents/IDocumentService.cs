namespace LegalDocumentAISearch.Application.Documents;

public interface IDocumentService
{
    Task<List<DocumentListItemDto>> ListDocumentsAsync(CancellationToken ct = default);
    Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken ct = default);
    Task<UploadDocumentResult> UploadDocumentAsync(UploadDocumentCommand command, CancellationToken ct = default);
    Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default);
}
