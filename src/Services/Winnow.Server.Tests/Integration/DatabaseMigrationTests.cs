using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Tests.Integration;

/// <summary>
/// Smoke tests that verify EF Core migrations apply cleanly for both database providers.
/// These tests catch schema drift, broken migrations, and provider-specific SQL issues.
/// </summary>
public class DatabaseMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgresContainer.StartAsync();
    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(string provider, string? postgresConnString = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["DatabaseProvider"] = provider,
            ["ConnectionStrings:Sqlite"] = "Data Source=:memory:",
            ["ConnectionStrings:Postgres"] = postgresConnString ?? "",
            ["Encryption:MasterKey"] = "dGVzdC1rZXktMzItYnl0ZXMtYmFzZTY0LW9r" // 32-byte base64 test key
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static WinnowDbContext CreateSqliteContext(SqliteConnection connection, IConfiguration config)
    {
        var options = new DbContextOptionsBuilder<WinnowDbContext>()
            .UseSqlite(connection)
            .Options;

        return new WinnowDbContext(options, new StubTenantContext(), config);
    }

    private static WinnowDbContext CreatePostgresContext(string connectionString, IConfiguration config)
    {
        var options = new DbContextOptionsBuilder<WinnowDbContext>()
            .UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly("Winnow.Server"))
            .Options;

        return new WinnowDbContext(options, new StubTenantContext(), config);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sqlite_Migrations_Apply_Successfully()
    {
        // Arrange
        var config = BuildConfig("Sqlite");
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var db = CreateSqliteContext(connection, config);

        // Act & Assert — MigrateAsync should not throw
        await db.Database.MigrateAsync();

        // Verify a known table exists
        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table' AND name='Reports'")
            .ToListAsync();

        Assert.Contains("Reports", tables);
    }

    [Fact]
    public async Task Postgres_Migrations_Apply_Successfully()
    {
        // Arrange
        var connectionString = _postgresContainer.GetConnectionString();
        var config = BuildConfig("Postgres", connectionString);

        using var db = CreatePostgresContext(connectionString, config);

        // Act & Assert — MigrateAsync should not throw
        await db.Database.MigrateAsync();

        // Verify a known table exists by querying information_schema
        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Reports'")
            .ToListAsync();

        Assert.Contains("Reports", tables);
    }

    // ── Stub ─────────────────────────────────────────────────────────────────

    private class StubTenantContext : ITenantContext
    {
        public string? TenantId { get; set; }
        public Guid? CurrentOrganizationId { get; set; }
        public string ConnectionString => "Data Source=:memory:";
    }
}
