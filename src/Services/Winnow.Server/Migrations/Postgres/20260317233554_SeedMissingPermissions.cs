using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SeedMissingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var permissions = new[]
            {
                ("integrations:read", "View third-party integration settings"),
                ("members:manage", "Invite and manage organization members"),
                ("members:read", "View organization members"),
                ("organizations:read", "View organization settings"),
                ("projects:manage", "Create and manage projects"),
                ("projects:read", "View project details"),
                ("teams:read", "View teams"),
                ("teams:write", "Manage teams"),
                ("roles:manage", "Manage organization roles")
            };

            var permissionIds = new Dictionary<string, Guid>();
            foreach (var (name, description) in permissions)
            {
                var id = Guid.NewGuid();
                permissionIds[name] = id;
                migrationBuilder.Sql($@"INSERT INTO ""Permissions"" (""Id"", ""Name"", ""Description"") VALUES ('{id}', '{name}', '{description}') ON CONFLICT (""Name"") DO NOTHING;");
            }

            // Assign all missing ones to Owner
            foreach (var (name, _) in permissions)
            {
                migrationBuilder.Sql($@"
                    INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") 
                    SELECT r.""Id"", p.""Id"" 
                    FROM ""Roles"" r
                    JOIN ""Permissions"" p ON p.""Name"" = '{name}'
                    WHERE r.""Name"" = 'Owner'
                    ON CONFLICT DO NOTHING;");
            }

            // Assign relevant ones to Admin
            var adminPermissions = new[] { "integrations:read", "members:read", "organizations:read", "projects:read", "teams:read", "roles:manage" };
            foreach (var name in adminPermissions)
            {
                migrationBuilder.Sql($@"
                    INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"") 
                    SELECT r.""Id"", p.""Id"" 
                    FROM ""Roles"" r
                    JOIN ""Permissions"" p ON p.""Name"" = '{name}'
                    WHERE r.""Name"" = 'Admin'
                    ON CONFLICT DO NOTHING;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var names = "'integrations:read', 'members:manage', 'members:read', 'organizations:read', 'projects:manage', 'projects:read', 'teams:read', 'teams:write', 'roles:manage'";
            migrationBuilder.Sql($@"DELETE FROM ""RolePermissions"" WHERE ""PermissionId"" IN (SELECT ""Id"" FROM ""Permissions"" WHERE ""Name"" IN ({names}));");
            migrationBuilder.Sql($@"DELETE FROM ""Permissions"" WHERE ""Name"" IN ({names});");
        }
    }
}
