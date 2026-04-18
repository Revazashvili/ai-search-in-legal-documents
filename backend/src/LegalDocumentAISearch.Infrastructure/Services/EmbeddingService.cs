using LegalDocumentAISearch.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;

namespace LegalDocumentAISearch.Infrastructure.Services;

public class EmbeddingService(OpenAIClient openAiClient, IConfiguration configuration) : IEmbeddingService
{
    private readonly string _model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

    public async Task<float[][]> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var client = openAiClient.GetEmbeddingClient(_model);
        const int batchSize = 100;
        var results = new List<float[]>();

        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var response = await client.GenerateEmbeddingsAsync(batch, cancellationToken: ct);
            results.AddRange(response.Value.Select(e => e.ToFloats().ToArray()));
        }

        return [.. results];
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var embeddings = await GenerateEmbeddingsAsync([text], ct);
        return embeddings[0];
    }
}
