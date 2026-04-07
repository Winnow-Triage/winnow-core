using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RefactorNotificationsAndAutoExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CriticalityThreshold",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotificationThreshold",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoExportEnabled",
                table: "Integrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalityThreshold",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "NotificationThreshold",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AutoExportEnabled",
                table: "Integrations");
        }
    }
}
