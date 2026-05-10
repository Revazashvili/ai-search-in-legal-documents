using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LegalDocumentAISearch.Infrastructure.Services;

public class AiConfigValidationService(
    IConfiguration configuration,
    ILogger<AiConfigValidationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var baseUrl = configuration["OpenAI:BaseUrl"];
        var embeddingModel = configuration["OpenAI:EmbeddingModel"];
        var chatModel = configuration["OpenAI:ChatModel"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("OpenAI:BaseUrl is not configured. Embedding and chat services will fail.");
            return;
        }

        logger.LogInformation("Validating AI config: BaseUrl={BaseUrl}, EmbeddingModel={EmbeddingModel}, ChatModel={ChatModel}",
            baseUrl, embeddingModel, chatModel);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            var response = await httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/models", cancellationToken);
            if (response.IsSuccessStatusCode)
                logger.LogInformation("AI endpoint reachable at {BaseUrl}", baseUrl);
            else
                logger.LogWarning("AI endpoint returned {StatusCode} at {BaseUrl}. Ingestion may fail.", response.StatusCode, baseUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot reach AI endpoint at {BaseUrl}. Ingestion will fail until the endpoint is available.", baseUrl);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
