using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceApiKeyWithHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApiKey",
                table: "Projects",
                newName: "ApiKeyHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApiKeyHash",
                table: "Projects",
                newName: "ApiKey");
        }
    }
}
