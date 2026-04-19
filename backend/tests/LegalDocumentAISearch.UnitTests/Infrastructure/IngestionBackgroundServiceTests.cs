using LegalDocumentAISearch.Application.Ingestion;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Infrastructure.Background;
using LegalDocumentAISearch.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LegalDocumentAISearch.UnitTests.Infrastructure;

public class IngestionBackgroundServiceTests
{
    private static (IngestionBackgroundService Service, IIngestionService IngestionService) BuildService(
        IIngestionQueue queue)
    {
        var ingestionService = Substitute.For<IIngestionService>();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(IIngestionService)).Returns(ingestionService);

        var asyncScope = Substitute.For<IAsyncDisposable>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateAsyncScope().Returns(new AsyncServiceScope(scope));

        var service = new IngestionBackgroundService(
            queue,
            scopeFactory,
            NullLogger<IngestionBackgroundService>.Instance);

        return (service, ingestionService);
    }

    [Fact]
    public async Task ExecuteAsync_SingleDocument_CallsIngestAsync()
    {
        var queue = new IngestionQueue();
        var (service, ingestionService) = BuildService(queue);
        var documentId = Guid.NewGuid();

        queue.Enqueue(documentId);

        using var cts = new CancellationTokenSource();
        var task = service.StartAsync(cts.Token);

        // Give the background service time to process
        await Task.Delay(100);
        await cts.CancelAsync();

        await ingestionService.Received().IngestAsync(documentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MultipleDocuments_CallsIngestForEach()
    {
        var queue = new IngestionQueue();
        var (service, ingestionService) = BuildService(queue);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        queue.Enqueue(id1);
        queue.Enqueue(id2);

        using var cts = new CancellationTokenSource();
        _ = service.StartAsync(cts.Token);

        await Task.Delay(200);
        await cts.CancelAsync();

        await ingestionService.Received().IngestAsync(id1, Arg.Any<CancellationToken>());
        await ingestionService.Received().IngestAsync(id2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_IngestThrows_ContinuesProcessingNextDocument()
    {
        var queue = new IngestionQueue();
        var (service, ingestionService) = BuildService(queue);
        var failingId = Guid.NewGuid();
        var successId = Guid.NewGuid();

        ingestionService
            .IngestAsync(failingId, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("ingestion failed"));

        queue.Enqueue(failingId);
        queue.Enqueue(successId);

        using var cts = new CancellationTokenSource();
        _ = service.StartAsync(cts.Token);

        await Task.Delay(200);
        await cts.CancelAsync();

        await ingestionService.Received().IngestAsync(successId, Arg.Any<CancellationToken>());
    }
}
