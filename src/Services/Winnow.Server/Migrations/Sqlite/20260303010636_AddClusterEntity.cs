using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Winnow.Server.Infrastructure.Persistence;

#nullable disable

namespace Winnow.Server.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddClusterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Clusters table
            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Centroid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    CriticalityScore = table.Column<int>(type: "INTEGER", nullable: true),
                    CriticalityReasoning = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            // Data migration: Create clusters from existing parent report hierarchies.
            // For each unique ParentReportId, create a Cluster from the parent report,
            // then point all children to the new cluster.
            migrationBuilder.Sql(@"
                -- Create Cluster records from reports that are parents (have children pointing to them)
                INSERT INTO Clusters (Id, ProjectId, OrganizationId, Centroid, Title, Summary, CriticalityScore, CriticalityReasoning, Status, CreatedAt)
                SELECT DISTINCT
                    p.Id,
                    p.ProjectId,
                    p.OrganizationId,
                    p.Embedding,
                    p.Title,
                    p.Summary,
                    p.CriticalityScore,
                    p.CriticalityReasoning,
                    CASE WHEN p.Status = 'Closed' THEN 'Closed' ELSE 'Open' END,
                    p.CreatedAt
                FROM Reports p
                WHERE p.Id IN (SELECT DISTINCT ParentReportId FROM Reports WHERE ParentReportId IS NOT NULL);

                -- Point child reports to their new cluster (same as ParentReportId)
                UPDATE Reports
                SET ClusterId = ParentReportId
                WHERE ParentReportId IS NOT NULL;

                -- Also assign the parent report itself to its own cluster
                UPDATE Reports
                SET ClusterId = Id
                WHERE Id IN (SELECT DISTINCT ParentReportId FROM Reports WHERE ParentReportId IS NOT NULL);
            ");

            // Add SuggestedClusterId column (renamed from SuggestedParentId)
            migrationBuilder.RenameColumn(
                name: "SuggestedParentId",
                table: "Reports",
                newName: "SuggestedClusterId");

            // Drop removed columns
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

            // Add FK + index for ClusterId
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalityScore",
                table: "Reports",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentReportId",
                table: "Reports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Reports",
                type: "TEXT",
                nullable: true);
        }
    }
}
