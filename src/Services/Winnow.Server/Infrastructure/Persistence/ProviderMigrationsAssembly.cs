using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Custom MigrationsAssembly that filters migrations by namespace prefix.
/// This enables dual-provider (SQLite + PostgreSQL) migrations in the same assembly;
/// each provider only discovers and applies migrations from its own namespace.
/// 
/// SQLite uses:  Winnow.Server.Migrations.Sqlite.*
/// Postgres uses: Winnow.Server.Migrations.Postgres.*
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage — required for multi-provider migration filtering
public class ProviderMigrationsAssembly : MigrationsAssembly
{
    private readonly string _namespacePrefix;

    public ProviderMigrationsAssembly(
        ICurrentDbContext currentContext,
        IDbContextOptions options,
        IMigrationsIdGenerator idGenerator,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
        : base(currentContext, options, idGenerator, logger)
    {
        // Determine the correct namespace based on the configured database provider.
        // Check the options extensions rather than Database.IsSqlite() because the
        // database connection may not be available during service construction.
        var isSqlite = options.Extensions
            .Any(e => e.GetType().FullName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true);

        _namespacePrefix = isSqlite
            ? "Winnow.Server.Migrations.Sqlite"
            : "Winnow.Server.Migrations.Postgres";
    }

    public override IReadOnlyDictionary<string, TypeInfo> Migrations
    {
        get
        {
            var allMigrations = base.Migrations;
            return allMigrations
                .Where(m => m.Value.Namespace?.StartsWith(_namespacePrefix, StringComparison.Ordinal) == true)
                .ToDictionary(m => m.Key, m => m.Value)
                .AsReadOnly();
        }
    }
}
#pragma warning restore EF1001
