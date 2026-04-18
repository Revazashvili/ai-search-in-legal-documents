using LegalDocumentAISearch.Application.Ingestion;
using LegalDocumentAISearch.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LegalDocumentAISearch.Infrastructure.Background;

public class IngestionBackgroundService(
    IIngestionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var documentId in queue.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();

            try
            {
                await ingestionService.IngestAsync(documentId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error ingesting document {Id}", documentId);
            }
        }
    }
}
