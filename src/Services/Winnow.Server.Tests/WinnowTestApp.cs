using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Tests;

/// <summary>
/// WebApplicationFactory for Winnow.Server integration tests.
/// Configures an in-memory SQLite database and test tenant context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WinnowTestApp"/> class.
/// </remarks>
/// <param name="configureTestServices">Optional action to configure services for testing.
/// Allows injecting custom mocks or service overrides per test.</param>
public class WinnowTestApp(Action<IServiceCollection>? configureTestServices = null) : WebApplicationFactory<Program>
{
    private readonly string _tenantId = "test-tenant";
    private SqliteConnection? _sqliteConnection;
    private readonly Action<IServiceCollection>? _configureTestServices = configureTestServices;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Allow test-specific service configuration
            _configureTestServices?.Invoke(services);
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
                options.AddInterceptors(new TestSqliteVectorConnectionInterceptor());
            });
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

        // Create a test organization
        var organizationId = Guid.NewGuid();
        var organization = new Winnow.Server.Entities.Organization
        {
            Id = organizationId,
            Name = "Test Organization",
            SubscriptionTier = "free",
            CreatedAt = DateTime.UtcNow
        };

        db.Organizations.Add(organization);
        
        // Add user as member of organization
        var organizationMember = new Winnow.Server.Entities.OrganizationMember
        {
            Id = Guid.NewGuid(),
            UserId = testUserId,
            OrganizationId = organizationId,
            Role = "owner",
            JoinedAt = DateTime.UtcNow
        };
        
        db.OrganizationMembers.Add(organizationMember);

        var project = new Winnow.Server.Entities.Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            ApiKey = apiKey,
            OwnerId = testUserId,
            OrganizationId = organizationId,
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

    private class TestSqliteVectorConnectionInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            LoadExtension(connection);
            base.ConnectionOpened(connection, eventData);
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            LoadExtension(connection);
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        private static void LoadExtension(DbConnection connection)
        {
            if (connection is SqliteConnection sqliteConnection)
            {
                sqliteConnection.EnableExtensions(true);
                try
                {
                    // Try to load from base directory explicitly
                    var extensionPath = Path.Combine(AppContext.BaseDirectory, "vec0.so");
                    Console.WriteLine($"Attempting to load SQLite vec0 extension from: {extensionPath}");
                    if (File.Exists(extensionPath))
                    {
                        sqliteConnection.LoadExtension(extensionPath);
                        Console.WriteLine("Successfully loaded vec0 extension from .so file");
                    }
                    else
                    {
                        // Fallback to just the name (works if extension is in system path)
                        Console.WriteLine("Attempting to load vec0 extension by name");
                        sqliteConnection.LoadExtension("vec0");
                        Console.WriteLine("Successfully loaded vec0 extension by name");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load sqlite-vec extension: {ex.Message}");
                    // Don't rethrow - allow tests to continue without vector search
                    // The consumer will handle missing extension gracefully
                }
            }
        }
    }
}
