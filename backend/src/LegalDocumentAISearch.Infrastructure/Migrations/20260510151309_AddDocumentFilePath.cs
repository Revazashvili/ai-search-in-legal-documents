using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LegalDocumentAISearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentFilePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Documents");
        }
    }
}
