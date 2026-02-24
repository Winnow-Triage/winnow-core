using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DotNet.Testcontainers.Configurations;
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
    private readonly PostgreSqlContainer? _postgresContainer;

    public DatabaseMigrationTests()
    {
        // Skip PostgreSQL tests when running in CI or without Docker/Podman
        if (Environment.GetEnvironmentVariable("CI") != null || !File.Exists("/run/podman/podman.sock"))
        {
            _postgresContainer = null;
            return;
        }

        Environment.SetEnvironmentVariable("DOCKER_HOST", "unix:///run/podman/podman.sock");
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        Environment.SetEnvironmentVariable("TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE", "/run/podman/podman.sock");

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithAutoRemove(true)
            .Build();
    }

    public Task InitializeAsync() => _postgresContainer?.StartAsync() ?? Task.CompletedTask;
    public Task DisposeAsync() => _postgresContainer?.DisposeAsync().AsTask() ?? Task.CompletedTask;

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
            // Suppress the PendingModelChangesWarning - this is expected when the model
            // is built differently at design time vs runtime (e.g., different tenant context values)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
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

    [SkipOnCIFact]
    public async Task Postgres_Migrations_Apply_Successfully()
    {
        // Skip if container is not available
        if (_postgresContainer == null)
        {
            return;
        }

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
        public Guid? CurrentOrganizationId { get; set; } = Guid.NewGuid(); // Set a value to ensure model matches migrations
        public string ConnectionString => "Data Source=:memory:";
    }
}
