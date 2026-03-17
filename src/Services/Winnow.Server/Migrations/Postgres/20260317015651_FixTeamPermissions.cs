using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FixTeamPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var teamsReadId = Guid.NewGuid();
            var teamsWriteId = Guid.NewGuid();

            migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{teamsReadId}', 'teams:read', 'View teams and their members');");
            migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{teamsWriteId}', 'teams:write', 'Create, edit and delete teams');");

            // Attach to Owner
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{teamsReadId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner';");
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{teamsWriteId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner';");

            // Attach to Admin
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{teamsReadId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Admin';");
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{teamsWriteId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Admin';");

            // Attach to Member (read only)
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{teamsReadId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Member';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Permissions"" WHERE ""Name"" IN ('teams:read', 'teams:write');");
        }
    }
}
