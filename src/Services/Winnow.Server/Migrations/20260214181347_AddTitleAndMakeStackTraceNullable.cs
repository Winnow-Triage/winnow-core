using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleAndMakeStackTraceNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Reports",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Backfill existing reports: copy Message into Title
            migrationBuilder.Sql("UPDATE Reports SET Title = Message WHERE Title = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Reports");
        }
    }
}
