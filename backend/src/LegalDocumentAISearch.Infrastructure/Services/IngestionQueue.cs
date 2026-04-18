using System.Threading.Channels;
using LegalDocumentAISearch.Application.Interfaces;

namespace LegalDocumentAISearch.Infrastructure.Services;

public class IngestionQueue : IIngestionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid documentId) => _channel.Writer.TryWrite(documentId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
