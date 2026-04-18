using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace LegalDocumentAISearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentsAndChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgvector extension
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SourceLawName = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateEnacted = table.Column<DateOnly>(type: "date", nullable: true),
                    LastAmended = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    RawText = table.Column<string>(type: "text", nullable: false),
                    ChunkingStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentChunkId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChunkType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Chunk"),
                    ArticleNumber = table.Column<string>(type: "text", nullable: true),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    ChunkText = table.Column<string>(type: "text", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_DocumentChunks_ParentChunkId",
                        column: x => x.ParentChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId",
                table: "DocumentChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_ParentChunkId",
                table: "DocumentChunks",
                column: "ParentChunkId");

            // Add tsvector column for full-text search (database-managed, not mapped in EF)
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""TsVector"" tsvector;");

            // Create tsvector update trigger function
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION update_tsvector() RETURNS trigger AS $$
                BEGIN
                    NEW."TsVector" := to_tsvector('simple', NEW."RawText");
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            // Attach trigger to Documents table
            migrationBuilder.Sql("""
                CREATE TRIGGER tsvector_update
                BEFORE INSERT OR UPDATE OF "RawText" ON "Documents"
                FOR EACH ROW EXECUTE FUNCTION update_tsvector();
                """);

            // GIN index on TsVector for fast full-text search
            migrationBuilder.Sql(@"CREATE INDEX idx_documents_tsvector ON ""Documents"" USING GIN (""TsVector"");");

            // HNSW index on Embedding for fast cosine similarity search
            migrationBuilder.Sql("""
                CREATE INDEX idx_chunks_embedding ON "DocumentChunks"
                USING hnsw ("Embedding" vector_cosine_ops)
                WHERE "Embedding" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_chunks_embedding;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS idx_documents_tsvector;");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS tsvector_update ON ""Documents"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_tsvector();");

            migrationBuilder.DropTable(
                name: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
