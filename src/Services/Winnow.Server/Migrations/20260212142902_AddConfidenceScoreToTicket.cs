using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddConfidenceScoreToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ConfidenceScore",
                table: "Tickets",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "Tickets");
        }
    }
}
