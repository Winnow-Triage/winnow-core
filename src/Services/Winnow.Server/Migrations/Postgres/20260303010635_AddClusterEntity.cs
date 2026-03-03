using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddClusterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Clusters table first
            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Centroid = table.Column<Vector>(type: "vector(384)", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    CriticalityScore = table.Column<int>(type: "integer", nullable: true),
                    CriticalityReasoning = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clusters_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_ProjectId",
                table: "Clusters",
                column: "ProjectId");

            // 2. Data migration: convert existing parent-report hierarchies into Cluster records
            //    Must happen BEFORE dropping ParentReportId/Summary/Criticality columns
            migrationBuilder.Sql(@"
                -- Create Cluster records from reports that are parents (have children pointing to them)
                INSERT INTO ""Clusters"" (""Id"", ""ProjectId"", ""OrganizationId"", ""Centroid"", ""Title"", ""Summary"", ""CriticalityScore"", ""CriticalityReasoning"", ""Status"", ""CreatedAt"")
                SELECT DISTINCT
                    p.""Id"",
                    p.""ProjectId"",
                    p.""OrganizationId"",
                    p.""Embedding"",
                    p.""Title"",
                    p.""Summary"",
                    p.""CriticalityScore"",
                    p.""CriticalityReasoning"",
                    CASE WHEN p.""Status"" = 'Closed' THEN 'Closed' ELSE 'Open' END,
                    p.""CreatedAt""
                FROM ""Reports"" p
                WHERE p.""Id"" IN (SELECT DISTINCT ""ParentReportId"" FROM ""Reports"" WHERE ""ParentReportId"" IS NOT NULL);

                -- Point child reports to their new cluster (same as ParentReportId)
                UPDATE ""Reports""
                SET ""ClusterId"" = ""ParentReportId""
                WHERE ""ParentReportId"" IS NOT NULL;

                -- Also assign the parent report itself to its own cluster
                UPDATE ""Reports""
                SET ""ClusterId"" = ""Id""
                WHERE ""Id"" IN (SELECT DISTINCT ""ParentReportId"" FROM ""Reports"" WHERE ""ParentReportId"" IS NOT NULL);
            ");

            // 3. Now safe to drop old columns
            migrationBuilder.DropColumn(
                name: "CriticalityReasoning",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "CriticalityScore",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ParentReportId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Reports");

            // 4. Rename SuggestedParentId -> SuggestedClusterId
            migrationBuilder.RenameColumn(
                name: "SuggestedParentId",
                table: "Reports",
                newName: "SuggestedClusterId");

            // 5. Add FK + index for ClusterId
            migrationBuilder.CreateIndex(
                name: "IX_Reports_ClusterId",
                table: "Reports",
                column: "ClusterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Clusters_ClusterId",
                table: "Reports",
                column: "ClusterId",
                principalTable: "Clusters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Clusters_ClusterId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "Clusters");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ClusterId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "SuggestedClusterId",
                table: "Reports",
                newName: "SuggestedParentId");

            migrationBuilder.AddColumn<string>(
                name: "CriticalityReasoning",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalityScore",
                table: "Reports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentReportId",
                table: "Reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Reports",
                type: "text",
                nullable: true);
        }
    }
}
