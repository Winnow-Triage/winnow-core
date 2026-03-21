using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.API.Migrations.Postgres
{
    public partial class GrantBroadAdminPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Grant ALL permissions to Owner and Admin roles
            migrationBuilder.Sql(@"
                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r
                CROSS JOIN ""Permissions"" p
                WHERE r.""Name"" IN ('Owner', 'Admin')
                ON CONFLICT DO NOTHING;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No simple way to undo this without tracking what was there before, 
            // but these roles are intended to be broad.
        }
    }
}
