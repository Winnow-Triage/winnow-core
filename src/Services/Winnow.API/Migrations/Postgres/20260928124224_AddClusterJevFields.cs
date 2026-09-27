using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddClusterJevFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "CriticalityScore",
                table: "Clusters",
                newName: "Severity");

            migrationBuilder.RenameColumn(
                name: "CriticalityReasoning",
                table: "Clusters",
                newName: "IssueType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Severity",
                table: "Clusters",
                newName: "CriticalityScore");

            migrationBuilder.RenameColumn(
                name: "IssueType",
                table: "Clusters",
                newName: "CriticalityReasoning");

            migrationBuilder.AddColumn<string>(
                name: "IssueType",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "Reports",
                type: "integer",
                nullable: true);
        }
    }
}
