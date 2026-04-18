namespace LegalDocumentAISearch.Application.Interfaces;

public interface IIngestionQueue
{
    void Enqueue(Guid documentId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct);
}
