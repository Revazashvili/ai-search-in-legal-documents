using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalDocumentAISearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEmbeddingDimTo768 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ALTER COLUMN TYPE is not reliable for pgvector dimension changes.
            // Drop and re-add the column — the HNSW index is automatically dropped with it.
            migrationBuilder.Sql(@"ALTER TABLE ""DocumentChunks"" DROP COLUMN IF EXISTS ""Embedding"";");
            migrationBuilder.Sql(@"ALTER TABLE ""DocumentChunks"" ADD COLUMN ""Embedding"" vector(768);");
            migrationBuilder.Sql(@"
                CREATE INDEX idx_chunks_embedding ON ""DocumentChunks""
                USING hnsw (""Embedding"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""DocumentChunks"" DROP COLUMN IF EXISTS ""Embedding"";");
            migrationBuilder.Sql(@"ALTER TABLE ""DocumentChunks"" ADD COLUMN ""Embedding"" vector(1536);");
            migrationBuilder.Sql(@"
                CREATE INDEX idx_chunks_embedding ON ""DocumentChunks""
                USING hnsw (""Embedding"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);");
        }
    }
}
