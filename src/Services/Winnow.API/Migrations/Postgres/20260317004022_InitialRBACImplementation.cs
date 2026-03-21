using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialRBACImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Description first before doing role stuff

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "text",
                nullable: true);

            // Add RoleId columns first but nullable so we can populate them
            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "OrganizationMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "OrganizationInvitations",
                type: "uuid",
                nullable: true);

            // Seed System Roles
            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            migrationBuilder.Sql($"INSERT INTO \"Roles\" (\"Id\", \"Name\") VALUES ('{ownerId}', 'Owner');");
            migrationBuilder.Sql($"INSERT INTO \"Roles\" (\"Id\", \"Name\") VALUES ('{adminId}', 'Admin');");
            migrationBuilder.Sql($"INSERT INTO \"Roles\" (\"Id\", \"Name\") VALUES ('{memberId}', 'Member');");

            // Update existing Members using their string Role mapping
            migrationBuilder.Sql($@"
                UPDATE ""OrganizationMembers"" 
                SET ""RoleId"" = CASE 
                    WHEN ""Role"" ILIKE 'owner%' THEN '{ownerId}'::uuid
                    WHEN ""Role"" ILIKE 'admin%' THEN '{adminId}'::uuid
                    ELSE '{memberId}'::uuid
                END;
            ");

            // Update existing Invitations
            migrationBuilder.Sql($@"
                UPDATE ""OrganizationInvitations"" 
                SET ""RoleId"" = CASE 
                    WHEN ""Role"" ILIKE 'owner%' THEN '{ownerId}'::uuid
                    WHEN ""Role"" ILIKE 'admin%' THEN '{adminId}'::uuid
                    ELSE '{memberId}'::uuid
                END;
            ");

            // Now we can safely drop the old string columns
            migrationBuilder.DropColumn(
                name: "Role",
                table: "OrganizationMembers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "OrganizationInvitations");

            // Now make RoleId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "OrganizationMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "OrganizationInvitations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_RoleId",
                table: "OrganizationMembers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_RoleId",
                table: "OrganizationInvitations",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationInvitations_Roles_RoleId",
                table: "OrganizationInvitations",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMembers_Roles_RoleId",
                table: "OrganizationMembers",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationInvitations_Roles_RoleId",
                table: "OrganizationInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMembers_Roles_RoleId",
                table: "OrganizationMembers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMembers_RoleId",
                table: "OrganizationMembers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationInvitations_RoleId",
                table: "OrganizationInvitations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "OrganizationMembers");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "OrganizationInvitations");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "OrganizationMembers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "OrganizationInvitations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
