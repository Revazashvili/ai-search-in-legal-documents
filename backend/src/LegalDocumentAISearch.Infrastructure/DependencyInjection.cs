using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Application.Ingestion;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Application.Search;
using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.Infrastructure.Background;
using LegalDocumentAISearch.Infrastructure.Persistence;
using LegalDocumentAISearch.Infrastructure.Repositories;
using LegalDocumentAISearch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace LegalDocumentAISearch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<LegalDocumentsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o =>
                {
                    o.MigrationsAssembly(typeof(LegalDocumentsDbContext).Assembly.FullName);
                    o.MigrationsHistoryTable("__EFMigrationsHistory");
                    o.UseVector();
                }));

        // Identity
        services.AddIdentityApiEndpoints<AdminUser>()
            .AddEntityFrameworkStores<LegalDocumentsDbContext>();

        // OpenAI-compatible client — works with OpenAI or any compatible endpoint (e.g. Ollama)
        services.AddSingleton(sp =>
        {
            var apiKey = configuration["OpenAI:ApiKey"] ?? "ollama";
            var baseUrl = configuration["OpenAI:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
                return new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options);
            }
            return new OpenAIClient(apiKey);
        });

        // Infrastructure implementations of Application interfaces
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IRagChatService, RagChatService>();

        // Ingestion queue: singleton so the background service and request handlers share the same channel
        services.AddSingleton<IIngestionQueue, IngestionQueue>();

        // Application use-case services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IIngestionService, IngestionService>();

        // Background worker
        services.AddHostedService<IngestionBackgroundService>();

        return services;
    }
}
