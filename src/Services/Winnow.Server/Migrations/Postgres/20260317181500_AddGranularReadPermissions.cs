using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    public partial class AddGranularReadPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var permissions = new[]
            {
                ("projects:read", "View project details and settings"),
                ("organizations:read", "View organization details"),
                ("members:read", "View organization members"),
                ("integrations:read", "View integration configurations")
            };

            var permissionIds = new Dictionary<string, Guid>();
            foreach (var (name, description) in permissions)
            {
                var id = Guid.NewGuid();
                permissionIds[name] = id;
                migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{id}', '{name}', '{description}') ON CONFLICT DO NOTHING;");
            }

            // Assign all read permissions to Owner, Admin, and Member
            foreach (var (name, _) in permissions)
            {
                var id = permissionIds[name];
                // Owner
                migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{id}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Owner' ON CONFLICT DO NOTHING;");
                // Admin
                migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{id}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Admin' ON CONFLICT DO NOTHING;");
                // Member
                migrationBuilder.Sql($@"INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") SELECT ""Id"", '{id}'::uuid FROM ""Roles"" WHERE ""Name"" = 'Member' ON CONFLICT DO NOTHING;");
            }

            // Explicitly ensure Admin has management permissions that might have been missed
            var adminManagePermissions = new[] { "projects:manage", "integrations:manage", "billing:manage" };
            foreach (var name in adminManagePermissions)
            {
                migrationBuilder.Sql($@"
                    INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") 
                    SELECT r.""Id"", p.""Id"" 
                    FROM ""Roles"" r
                    CROSS JOIN ""Permissions"" p
                    WHERE r.""Name"" = 'Admin' AND p.""Name"" = '{name}'
                    ON CONFLICT DO NOTHING;
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Permissions"" WHERE ""Name"" IN ('projects:read', 'organizations:read', 'members:read', 'integrations:read');");
        }
    }
}
