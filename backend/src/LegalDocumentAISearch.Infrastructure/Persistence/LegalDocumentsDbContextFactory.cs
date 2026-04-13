using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LegalDocumentAISearch.Infrastructure.Persistence;

public class LegalDocumentsDbContextFactory : IDesignTimeDbContextFactory<LegalDocumentsDbContext>
{
    public LegalDocumentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LegalDocumentsDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=legaldocumentaisearch;Username=postgres;Password=postgres",
                o => o.MigrationsAssembly(typeof(LegalDocumentsDbContext).Assembly.FullName)
                       .MigrationsHistoryTable("__EFMigrationsHistory"))
            .Options;

        return new LegalDocumentsDbContext(options);
    }
}
