using LegalDocumentAISearch.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LegalDocumentAISearch.Infrastructure.Persistence;

public class LegalDocumentsDbContext(DbContextOptions<LegalDocumentsDbContext> options)
    : IdentityDbContext<AdminUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LegalDocumentsDbContext).Assembly);

        modelBuilder.Entity<AdminUser>().ToTable("Users", "admin");
        modelBuilder.Entity<IdentityRole>().ToTable("Roles", "admin");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "admin");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "admin");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "admin");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "admin");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "admin");
    }
}
