using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Tests;

/// <summary>
/// WebApplicationFactory for Winnow.Server integration tests.
/// Uses a shared PostgreSQL Testcontainer from PostgresFixture.
/// </summary>
public class WinnowTestApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _tenantId = "test-tenant";
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly string _connectionString;

    public WinnowTestApp(PostgresFixture fixture, Action<IServiceCollection>? configureTestServices = null)
    {
        _connectionString = fixture.ConnectionString;
        _configureTestServices = configureTestServices;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Don't stop the container here, as it's shared.
        await Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            _configureTestServices?.Invoke(services);

            // Remove existing DbContext registrations
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<WinnowDbContext>) ||
                d.ServiceType == typeof(WinnowDbContext) ||
                d.ServiceType == typeof(ITenantContext)).ToList();

            foreach (var d in descriptors) services.Remove(d);

            // Remove hosted services to prevent background interference
            var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
            foreach (var s in hostedServices) services.Remove(s);

            var connString = _connectionString ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                            ?? throw new InvalidOperationException("PostgreSQL connection string not found.");

            // Register test tenant context
            services.AddSingleton<ITenantContext>(new TestTenantContext(_tenantId, connString));

            // Register DbContext with Npgsql
            services.AddDbContext<WinnowDbContext>(options =>
            {
                options.UseNpgsql(connString, npgsql =>
                {
                    npgsql.UseVector();
                    npgsql.MigrationsAssembly("Winnow.Server");
                });

                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
        });
    }

    /// <summary>
    /// Creates a test project in the database with a known API key for authentication.
    /// Returns the generated API key for use in tests.
    /// </summary>
    public async Task<(Guid ProjectId, string ApiKey)> CreateTestProjectAsync(string? userEmail = null, string? userPassword = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Winnow.Server.Entities.ApplicationUser>>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<Winnow.Server.Infrastructure.Security.IApiKeyService>();

        // Create a dummy user for the project owner
        var testUserId = Guid.NewGuid().ToString();
        var testUserEmail = userEmail ?? "test@example.com";
        var testUserPassword = userPassword ?? "Password123!";

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == testUserEmail);
        if (existingUser == null)
        {
            var user = new Winnow.Server.Entities.ApplicationUser
            {
                Id = testUserId,
                UserName = testUserEmail,
                Email = testUserEmail,
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(user, testUserPassword);
            existingUser = user;
        }

        var organizationId = Guid.NewGuid();
        var organization = new Winnow.Server.Entities.Organization
        {
            Id = organizationId,
            Name = "Test Organization",
            SubscriptionTier = "free",
            CreatedAt = DateTime.UtcNow
        };
        db.Organizations.Add(organization);

        var organizationMember = new Winnow.Server.Entities.OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = existingUser.Id,
            OrganizationId = organizationId,
            Role = "owner",
            JoinedAt = DateTime.UtcNow
        };
        db.OrganizationMembers.Add(organizationMember);

        var projectId = Guid.NewGuid();
        var generatedApiKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Winnow.Server.Entities.Project
        {
            Id = projectId,
            Name = "Test Project",
            ApiKeyHash = apiKeyService.HashKey(generatedApiKey),
            OwnerId = existingUser.Id,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (projectId, generatedApiKey);
    }

    /// <summary>
    /// Resets the database by truncating all tables (preserving schema).
    /// This avoids "cannot drop the currently open database" when sharing a container.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        // Ensure migrations have been applied
        await db.Database.MigrateAsync();

        // Truncate all user tables in the public schema (CASCADE to handle FK constraints)
        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename != '__EFMigrationsHistory'")
            .ToListAsync();

        if (tables.Count > 0)
        {
            var tableList = string.Join(", ", tables.Select(t => $"\"{t}\""));
            var sql = $"TRUNCATE TABLE {tableList} CASCADE";
#pragma warning disable EF1002 // Table names from pg_tables, not user input
            await db.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
        }
    }

    private class TestTenantContext : Winnow.Server.Infrastructure.MultiTenancy.TenantContext
    {
        private readonly string _connectionString;

        public TestTenantContext(string tenantId, string connectionString)
            : base(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Postgres",
                ["ConnectionStrings:Postgres"] = connectionString
            }).Build())
        {
            TenantId = tenantId;
            _connectionString = connectionString;
        }

        public override string ConnectionString => _connectionString;
    }
}
