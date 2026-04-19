using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Infrastructure.Persistence;
using NSubstitute;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LegalDocumentAISearch.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private NpgsqlDataSource? _dataSource;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public IEmbeddingService EmbeddingService { get; } = Substitute.For<IEmbeddingService>();
    public IRagChatService RagChatService { get; } = Substitute.For<IRagChatService>();
    public IPdfTextExtractor PdfTextExtractor { get; } = Substitute.For<IPdfTextExtractor>();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Build a proper NpgsqlDataSource with pgvector type mapping
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with test DB connection using the shared data source
                services.RemoveAll<DbContextOptions<LegalDocumentsDbContext>>();
                services.RemoveAll<NpgsqlDataSource>();
                services.AddSingleton(_dataSource);

                services.AddDbContext<LegalDocumentsDbContext>(options =>
                    options.UseNpgsql(
                        _dataSource,
                        o =>
                        {
                            o.MigrationsAssembly(typeof(LegalDocumentsDbContext).Assembly.FullName);
                            o.UseVector();
                        }));

                // Replace AI services with mocks (no real server needed)
                services.RemoveAll<IEmbeddingService>();
                services.AddScoped(_ => EmbeddingService);
                services.RemoveAll<IRagChatService>();
                services.AddScoped(_ => RagChatService);
                services.RemoveAll<IPdfTextExtractor>();
                services.AddScoped(_ => PdfTextExtractor);
            });
        });

        // Run migration explicitly so the vector extension is installed and type OIDs are registered
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LegalDocumentsDbContext>();
        await db.Database.MigrateAsync();

        // Force Npgsql to reload its type cache so vector type OID is available after CREATE EXTENSION
        await using var conn = await _dataSource.OpenConnectionAsync();
        await conn.ReloadTypesAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public LegalDocumentsDbContext CreateDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<LegalDocumentsDbContext>();
    }
}
