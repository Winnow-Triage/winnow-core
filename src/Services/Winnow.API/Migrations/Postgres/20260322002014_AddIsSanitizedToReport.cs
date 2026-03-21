using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddIsSanitizedToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSanitized",
                table: "Reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSanitized",
                table: "Reports");
        }
    }
}
