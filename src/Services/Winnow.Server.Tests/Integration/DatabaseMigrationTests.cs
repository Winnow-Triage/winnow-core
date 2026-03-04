using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;
using Xunit;

namespace Winnow.Server.Tests.Integration;

[Collection("PostgresCollection")]
public class DatabaseMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public DatabaseMigrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

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

    private static WinnowDbContext CreatePostgresContext(string connectionString, IConfiguration config)
    {
        var options = new DbContextOptionsBuilder<WinnowDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.MigrationsAssembly("Winnow.Server");
            })
            .Options;

        return new WinnowDbContext(options, new StubTenantContext(connectionString), config);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [SkipOnCIFact]
    public async Task Postgres_Migrations_Apply_Successfully()
    {
        // Arrange
        var connectionString = _fixture.ConnectionString;
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
        private readonly string _connectionString;
        public StubTenantContext(string connectionString) => _connectionString = connectionString;
        public string? TenantId { get; set; }
        public Guid? CurrentOrganizationId { get; set; } = Guid.NewGuid();
        public string ConnectionString => _connectionString;
    }
}
