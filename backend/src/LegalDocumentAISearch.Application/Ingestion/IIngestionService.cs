namespace LegalDocumentAISearch.Application.Ingestion;

public interface IIngestionService
{
    Task IngestAsync(Guid documentId, CancellationToken ct = default);
}
