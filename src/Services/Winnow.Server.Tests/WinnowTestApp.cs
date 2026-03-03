using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
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
        // Use a non-Development environment so appsettings.Development.json
        // (which sets "DatabaseProvider": "Postgres") is never loaded.
        // This prevents the production ServiceExtensions from registering Npgsql.
        builder.UseEnvironment("Testing");

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

            // Remove all hosted services to prevent background processing and seeding races during tests.
            // These services (like InvitationCleanupJob and AdminSeeder) try to query the DB on startup
            // before we've had a chance to call EnsureCreatedAsync in the test itself.
            var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // Create and open a SHARED in-memory SQLite connection
            // Using a unique file name with memory mode ensures test instances don't share the same DB
            _sqliteConnection = new SqliteConnection($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared");
            _sqliteConnection.Open();

            // Disable foreign key constraints for testing to avoid needing to create related entities
            using (var command = _sqliteConnection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = OFF;";
                command.ExecuteNonQuery();
            }

            var connectionString = _sqliteConnection.ConnectionString;

            // Register a test tenant context that returns our in-memory connection
            services.AddSingleton<ITenantContext>(new TestTenantContext(_tenantId, connectionString));

            // Register DbContext with the connection string
            services.AddDbContext<WinnowDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(connectionString);
                options.AddInterceptors(new TestSqliteVectorConnectionInterceptor());
                // Suppress PendingModelChangesWarning — the test uses EnsureCreated,
                // so out-of-date migration snapshots are irrelevant.
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
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
    /// Returns the generated API key for use in tests.
    /// </summary>
    public async Task<(Guid ProjectId, string ApiKey)> CreateTestProjectAsync(string? userEmail = null, string? userPassword = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Winnow.Server.Entities.ApplicationUser>>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<Winnow.Server.Infrastructure.Security.IApiKeyService>();

        // Ensure database is created
        await db.Database.EnsureCreatedAsync();

        // Create a dummy user for the project owner (since Project has required OwnerId foreign key)
        var testUserId = Guid.NewGuid().ToString();
        var testUserEmail = userEmail ?? "test@example.com";
        var testUserPassword = userPassword ?? "Password123!"; // Default password for tests

        // Check if user already exists
        var existingUser = await db.Users.FindAsync(testUserId);
        if (existingUser == null)
        {
            // Use UserManager to create user with hashed password
            var user = new Winnow.Server.Entities.ApplicationUser
            {
                Id = testUserId,
                UserName = testUserEmail,
                Email = testUserEmail,
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, testUserPassword);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
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

        // Generate API key in the correct format: wm_live_{ProjectId}_{RandomSecret}
        var projectId = Guid.NewGuid();
        var generatedApiKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Winnow.Server.Entities.Project
        {
            Id = projectId,
            Name = "Test Project",
            ApiKeyHash = apiKeyService.HashKey(generatedApiKey),
            OwnerId = testUserId,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (projectId, generatedApiKey);
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

        // Re-disable foreign key constraints after recreation
        using (var command = _sqliteConnection!.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = OFF;";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Test implementation of ITenantContext that inherits from TenantContext to satisfy the cast in IngestReportEndpoint.
    /// </summary>
    private class TestTenantContext : Winnow.Server.Infrastructure.MultiTenancy.TenantContext
    {
        private readonly string _connectionString;

        public TestTenantContext(string tenantId, string connectionString)
            : base(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = connectionString
            }).Build())
        {
            TenantId = tenantId;
            _connectionString = connectionString;
        }

        public override string ConnectionString => _connectionString;
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
