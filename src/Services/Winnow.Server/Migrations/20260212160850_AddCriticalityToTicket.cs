using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalityToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CriticalityReasoning",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalityScore",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalityReasoning",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CriticalityScore",
                table: "Tickets");
        }
    }
}
