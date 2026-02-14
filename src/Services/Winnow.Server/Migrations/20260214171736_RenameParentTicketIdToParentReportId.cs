using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameParentTicketIdToParentReportId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sync-only migration: ParentReportId already exists in dev databases due to previous manual/accidental changes.
            /*
            migrationBuilder.RenameColumn(
                name: "ParentTicketId",
                table: "Reports",
                newName: "ParentReportId");
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParentReportId",
                table: "Reports",
                newName: "ParentTicketId");
        }
    }
}
