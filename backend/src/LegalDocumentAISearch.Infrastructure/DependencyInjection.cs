using LegalDocumentAISearch.Domain.Entities;
using LegalDocumentAISearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LegalDocumentAISearch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LegalDocumentsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.MigrationsAssembly(typeof(LegalDocumentsDbContext).Assembly.FullName)
                       .MigrationsHistoryTable("__EFMigrationsHistory")));

        services.AddIdentityApiEndpoints<AdminUser>()
            .AddEntityFrameworkStores<LegalDocumentsDbContext>();

        return services;
    }
}
