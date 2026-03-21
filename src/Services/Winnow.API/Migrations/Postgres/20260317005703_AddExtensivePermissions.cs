using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddExtensivePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var permissions = new[]
            {
                ("reports:read", "View reports"),
                ("reports:write", "Create and edit reports"),
                ("reports:delete", "Delete reports"),
                ("clusters:read", "View clusters"),
                ("clusters:write", "Manage and modify clusters"),
                ("clusters:delete", "Delete clusters"),
                ("teams:manage", "Create, edit, and delete teams"),
                ("billing:manage", "Manage subscription and billing details"),
                ("settings:manage", "Manage organization-wide settings"),
                ("apikeys:manage", "Create and manage API keys"),
                ("integrations:manage", "Manage third-party integrations"),
                ("auditlogs:read", "View organization audit logs")
            };

            var permissionIds = new Dictionary<string, Guid>();
            foreach (var (name, description) in permissions)
            {
                var id = Guid.NewGuid();
                permissionIds[name] = id;
                migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{id}', '{name}', '{description}');");
            }

            // Assign them all to Owner
            foreach (var (name, _) in permissions)
            {
                var id = permissionIds[name];
                migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{id}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner';");
            }

            // Assign some to Admin
            var adminPermissions = new[] { "reports:read", "reports:write", "clusters:read", "clusters:write", "teams:manage", "settings:manage", "integrations:manage", "auditlogs:read" };
            foreach (var name in adminPermissions)
            {
                var id = permissionIds[name];
                migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{id}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Admin';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var names = "'reports:read', 'reports:write', 'reports:delete', 'clusters:read', 'clusters:write', 'clusters:delete', 'teams:manage', 'billing:manage', 'settings:manage', 'apikeys:manage', 'integrations:manage', 'auditlogs:read'";
            migrationBuilder.Sql($@"DELETE FROM ""Permissions"" WHERE ""Name"" IN ({names});");
        }
    }
}
