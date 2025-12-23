using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archivsoftware.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlainTextToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlainText",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlainText",
                table: "Documents");
        }
    }
}
