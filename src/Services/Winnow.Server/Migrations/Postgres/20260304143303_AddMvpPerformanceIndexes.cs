using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddMvpPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_ProjectId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Clusters_ProjectId",
                table: "Clusters");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Embedding",
                table: "Reports",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_OrganizationId",
                table: "Reports",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ProjectId_Status_CreatedAt",
                table: "Reports",
                columns: new[] { "ProjectId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OrganizationId",
                table: "Integrations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_Centroid",
                table: "Clusters",
                column: "Centroid")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_OrganizationId",
                table: "Clusters",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_ProjectId_Status_CreatedAt",
                table: "Clusters",
                columns: new[] { "ProjectId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_OrganizationId",
                table: "Assets",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_Embedding",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_OrganizationId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ProjectId_Status_CreatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_OrganizationId",
                table: "Integrations");

            migrationBuilder.DropIndex(
                name: "IX_Clusters_Centroid",
                table: "Clusters");

            migrationBuilder.DropIndex(
                name: "IX_Clusters_OrganizationId",
                table: "Clusters");

            migrationBuilder.DropIndex(
                name: "IX_Clusters_ProjectId_Status_CreatedAt",
                table: "Clusters");

            migrationBuilder.DropIndex(
                name: "IX_Assets_OrganizationId",
                table: "Assets");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ProjectId",
                table: "Reports",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_ProjectId",
                table: "Clusters",
                column: "ProjectId");
        }
    }
}
