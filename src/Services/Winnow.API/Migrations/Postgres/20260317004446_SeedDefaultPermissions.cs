using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SeedDefaultPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var rolesManageId = Guid.NewGuid();
            var membersManageId = Guid.NewGuid();
            var projectsManageId = Guid.NewGuid();

            migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{rolesManageId}', 'roles:manage', 'Manage all custom roles and permissions');");
            migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{membersManageId}', 'members:manage', 'Invite and manage members of the organization');");
            migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{projectsManageId}', 'projects:manage', 'Create and configure projects within the organization');");

            // Attach permissions to Owner
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{rolesManageId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner';");
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{membersManageId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner';");
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{projectsManageId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner';");

            // Attach permissions to Admin
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{membersManageId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Admin';");
            migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{projectsManageId}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Admin';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Permissions"" WHERE ""Name"" IN ('roles:manage', 'members:manage', 'projects:manage');");
        }
    }
}
