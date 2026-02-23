using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class ScopeIntegrationsToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Integrations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_ProjectId",
                table: "Integrations",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_Projects_ProjectId",
                table: "Integrations",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_Projects_ProjectId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_ProjectId",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Integrations");
        }
    }
}
