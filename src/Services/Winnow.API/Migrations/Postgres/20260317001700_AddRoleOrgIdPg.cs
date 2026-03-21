using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddRoleOrgIdPg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name_OrganizationId",
                table: "Roles",
                columns: new[] { "Name", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_Name_OrganizationId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);
        }
    }
}
