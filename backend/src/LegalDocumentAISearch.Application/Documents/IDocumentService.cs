namespace LegalDocumentAISearch.Application.Documents;

public interface IDocumentService
{
    Task<PagedResult<DocumentListItemDto>> ListDocumentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PagedResult<DocumentListItemDto>> ListPublicDocumentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken ct = default);
    Task<UploadDocumentResult> UploadDocumentAsync(UploadDocumentCommand command, CancellationToken ct = default);
    Task<bool> DeleteDocumentAsync(Guid id, CancellationToken ct = default);
}
