using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LegalDocumentAISearch.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=legaldocumentaisearch;Username=postgres;Password=postgres",
                o => o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                       .MigrationsHistoryTable("__EFMigrationsHistory"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
