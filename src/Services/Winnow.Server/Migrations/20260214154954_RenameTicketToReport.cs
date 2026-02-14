using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    public partial class RenameTicketToReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the table
            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "Reports");

            // Rename existing columns
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Reports",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Reports",
                newName: "StackTrace");

            migrationBuilder.RenameColumn(
                name: "MetadataJson",
                table: "Reports",
                newName: "Metadata");

            // Add new columns
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Reports",
                type: "TEXT",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "ClusterId",
                table: "Reports",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Reports",
                newName: "MetadataJson");

            migrationBuilder.RenameColumn(
                name: "StackTrace",
                table: "Reports",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "Reports",
                newName: "Title");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "Reports");

            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "Tickets");
        }
    }
}
