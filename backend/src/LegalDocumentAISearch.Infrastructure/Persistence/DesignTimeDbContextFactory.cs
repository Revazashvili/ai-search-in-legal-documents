using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LegalDocumentAISearch.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LegalDocumentsDbContext>
{
    public LegalDocumentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LegalDocumentsDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=legaldocumentaisearch;Username=postgres;Password=postgres",
            o =>
            {
                o.MigrationsAssembly(typeof(LegalDocumentsDbContext).Assembly.FullName);
                o.MigrationsHistoryTable("__EFMigrationsHistory");
                o.UseVector();
            });

        return new LegalDocumentsDbContext(optionsBuilder.Options);
    }
}
