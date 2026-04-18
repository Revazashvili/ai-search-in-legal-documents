using LegalDocumentAISearch.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LegalDocumentAISearch.Infrastructure.Persistence;

public class LegalDocumentsDbContext(DbContextOptions<LegalDocumentsDbContext> options)
    : IdentityDbContext<AdminUser>(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

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

        // Document
        modelBuilder.Entity<Document>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.DocumentType).HasMaxLength(50);
            b.Property(d => d.ChunkingStrategy).HasMaxLength(50);
            b.Property(d => d.Status).HasMaxLength(20).HasDefaultValue("Pending");
        });

        // DocumentChunk + pgvector embedding
        var floatArrayToVector = new ValueConverter<float[]?, Pgvector.Vector?>(
            v => v == null ? null : new Pgvector.Vector(v),
            v => v == null ? null : v.Memory.ToArray());

        modelBuilder.Entity<DocumentChunk>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.ChunkType).HasMaxLength(20).HasDefaultValue("Chunk");

            b.Property(c => c.Embedding)
                .HasColumnType("vector(1536)")
                .HasConversion(floatArrayToVector);

            b.HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.ParentChunk)
                .WithMany(c => c.ChildChunks)
                .HasForeignKey(c => c.ParentChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
