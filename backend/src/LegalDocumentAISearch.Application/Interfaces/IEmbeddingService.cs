namespace LegalDocumentAISearch.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[][]> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
