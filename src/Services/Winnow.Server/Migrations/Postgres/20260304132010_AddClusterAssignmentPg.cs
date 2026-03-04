using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddClusterAssignmentPg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "Clusters",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "Clusters");
        }
    }
}
