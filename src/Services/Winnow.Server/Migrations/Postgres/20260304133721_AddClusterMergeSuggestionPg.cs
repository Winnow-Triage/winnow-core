using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddClusterMergeSuggestionPg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedMergeClusterId",
                table: "Clusters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "SuggestedMergeConfidenceScore",
                table: "Clusters",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestedMergeClusterId",
                table: "Clusters");

            migrationBuilder.DropColumn(
                name: "SuggestedMergeConfidenceScore",
                table: "Clusters");
        }
    }
}
