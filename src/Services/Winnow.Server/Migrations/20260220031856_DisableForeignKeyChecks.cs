using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Winnow.Server.Migrations
{
    /// <inheritdoc />
    public partial class DisableForeignKeyChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration exists solely to execute PRAGMA foreign_keys = 0
            // outside of a transaction, as required by SQLite
            migrationBuilder.Sql("PRAGMA foreign_keys = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-enable foreign keys when rolling back
            migrationBuilder.Sql("PRAGMA foreign_keys = 1;");
        }
    }
}
