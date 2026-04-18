using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LegalDocumentAISearch.Application.Ingestion;

public class IngestionService(
    IDocumentRepository documentRepository,
    IChunkingService chunkingService,
    IEmbeddingService embeddingService,
    ILogger<IngestionService> logger) : IIngestionService
{
    public async Task IngestAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await documentRepository.FindByIdAsync(documentId, ct);
        if (document is null)
        {
            logger.LogWarning("Document {Id} not found for ingestion", documentId);
            return;
        }

        await documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Processing, ct: ct);

        try
        {
            // Step 1: Chunk the document using the selected strategy
            var chunks = chunkingService.Chunk(documentId, document.RawText, document.ChunkingStrategy);

            // Step 2: Hierarchical strategy — embed only paragraph-level children, not article parents
            var chunksToEmbed = document.ChunkingStrategy == ChunkingStrategy.Hierarchical
                ? chunks.Where(c => c.ChunkType == "Paragraph").ToList()
                : chunks;

            // Step 3: Generate embeddings in batches
            if (chunksToEmbed.Count > 0)
            {
                var texts = chunksToEmbed.Select(c => c.ChunkText).ToList();
                var embeddings = await embeddingService.GenerateEmbeddingsAsync(texts, ct);
                for (int i = 0; i < chunksToEmbed.Count; i++)
                    chunksToEmbed[i].Embedding = embeddings[i];
            }

            // Step 4: Persist all chunks and mark document Ready
            await documentRepository.AddChunksAsync(chunks, ct);
            await documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Ready, ct: ct);

            logger.LogInformation("Document {Id} ingested successfully with {Count} chunks", documentId, chunks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ingestion failed for document {Id}", documentId);
            // Use CancellationToken.None — the original ct may be cancelled, but we must still persist the failure
            await documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Failed, ex.Message, CancellationToken.None);
        }
    }
}
