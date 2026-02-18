using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Tests;

/// <summary>
/// WebApplicationFactory for Winnow.Server integration tests.
/// Configures an in-memory SQLite database and test tenant context.
/// </summary>
public class WinnowTestApp : WebApplicationFactory<Program>
{
    private readonly string _tenantId = "test-tenant";
    private SqliteConnection? _sqliteConnection;
    private Action<IServiceCollection>? _configureTestServices;

    /// <summary>
    /// Creates a new WinnowTestApp with optional test service configuration.
    /// </summary>
    /// <param name="configureTestServices">Optional action to configure test services after base configuration.</param>
    public WinnowTestApp(Action<IServiceCollection>? configureTestServices = null)
    {
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WinnowDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextDescriptor2 = services.SingleOrDefault(
                d => d.ServiceType == typeof(WinnowDbContext));
            if (dbContextDescriptor2 != null)
            {
                services.Remove(dbContextDescriptor2);
            }

            // Remove the existing ITenantContext registration
            var tenantContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantContext));
            if (tenantContextDescriptor != null)
            {
                services.Remove(tenantContextDescriptor);
            }

            // Remove the ClusterRefinementJob hosted service to prevent background processing during tests
            var hostedServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                     d.ImplementationType?.Name == "ClusterRefinementJob");
            if (hostedServiceDescriptor != null)
            {
                services.Remove(hostedServiceDescriptor);
            }

            // Create and open a SHARED in-memory SQLite connection
            // Using Cache=Shared ensures all connections to ":memory:" use the same database
            _sqliteConnection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            _sqliteConnection.Open();

            // Disable foreign key constraints for testing to avoid needing to create related entities
            using (var command = _sqliteConnection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = OFF;";
                command.ExecuteNonQuery();
            }

            // Register a test tenant context that returns our in-memory connection
            services.AddSingleton<ITenantContext>(new TestTenantContext(_tenantId, _sqliteConnection));

            // Register DbContext with the existing connection (not a new connection string)
            services.AddDbContext<WinnowDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_sqliteConnection);
                options.AddInterceptors(new SqliteVectorConnectionInterceptor());
            });

            // Apply additional test service configuration
            _configureTestServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sqliteConnection?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Creates a test project in the database with a known API key for authentication.
    /// </summary>
    public async Task<Guid> CreateTestProjectAsync(string apiKey = "test-api-key")
    {
        using var scope = Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
        
        // Ensure database is created
        await db.Database.EnsureCreatedAsync();

        // Create a dummy user for the project owner (since Project has required OwnerId foreign key)
        // We'll use a known test user ID
        var testUserId = Guid.NewGuid().ToString();
        var testUser = new Winnow.Server.Entities.ApplicationUser
        {
            Id = testUserId,
            UserName = "test-user",
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        // Check if user already exists
        var existingUser = await db.Users.FindAsync(testUserId);
        if (existingUser == null)
        {
            db.Users.Add(testUser);
            await db.SaveChangesAsync();
        }

        var project = new Winnow.Server.Entities.Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            ApiKey = apiKey,
            OwnerId = testUserId,
            CreatedAt = DateTime.UtcNow
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project.Id;
    }

    /// <summary>
    /// Clears all data from the in-memory database.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
        
        // Delete all data but keep tables
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Test implementation of ITenantContext that inherits from TenantContext to satisfy the cast in IngestReportEndpoint.
    /// </summary>
    private class TestTenantContext : Winnow.Server.Infrastructure.MultiTenancy.TenantContext
    {
        private readonly SqliteConnection _connection;

        public TestTenantContext(string tenantId, SqliteConnection connection)
        {
            TenantId = tenantId;
            _connection = connection;
        }

        public override string ConnectionString => _connection.ConnectionString;
    }
}
