using FastEndpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Domain.Projects;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Tests;

public class WinnowTestApp : WebApplicationFactory<Winnow.API.IApiMarker>, IAsyncLifetime
{
    private readonly bool _enablePoW;
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly string _connectionString;
    private readonly string _tenantId = "test-tenant";

    public WinnowTestApp(PostgresFixture fixture, Action<IServiceCollection>? configureTestServices = null, bool enablePoW = false)
    {
        _enablePoW = enablePoW;
        _configureTestServices = configureTestServices;
        _connectionString = fixture.ConnectionString;

        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", fixture.ConnectionString);
        Environment.SetEnvironmentVariable("USE_IN_MEMORY_TRANSPORT", "true");
    }

    static WinnowTestApp()
    {
        // Disable configuration reload on change to prevent hitting inotify limits in Linux/Docker
        Environment.SetEnvironmentVariable("ASPNETCORE_hostBuilder_reloadConfigOnChange", "false");
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
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

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Disable file watching to prevent "inotify limit" errors in Linux/Docker environments
            foreach (var source in config.Sources.OfType<FileConfigurationSource>())
            {
                source.ReloadOnChange = false;
            }

            var connString = _connectionString ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                             ?? "Host=localhost;Database=winnow;Username=postgres;Password=postgres";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connString,
                ["USE_IN_MEMORY_TRANSPORT"] = "true"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            _configureTestServices?.Invoke(services);

            var markerType = Type.GetType("Microsoft.AspNetCore.Authorization.Policy.AuthorizationPolicyMarkerService, Microsoft.AspNetCore.Authorization.Policy");
            if (markerType != null)
            {
                var markerInstance = Activator.CreateInstance(markerType, nonPublic: true);
                if (markerInstance != null)
                {
                    services.AddSingleton(markerType, markerInstance);
                }
            }

            services.AddAuthorization();
            services.AddFastEndpoints();

            /*
            // Remove existing DbContext registrations
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<WinnowDbContext>) ||
                d.ServiceType == typeof(WinnowDbContext) ||
                d.ServiceType == typeof(ITenantContext)).ToList();

            foreach (var d in descriptors) services.Remove(d);
            */

            // Remove hosted services to prevent background interference during tests
            // except for vital framework services like Wolverine which is needed for IMessageBus
            var hostedServices = services.Where(d =>
            {
                if (d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) ||
                    (d.ImplementationType != null && d.ImplementationType.IsAssignableTo(typeof(Microsoft.Extensions.Hosting.IHostedService))))
                {
                    var implName = d.ImplementationType?.FullName ?? "";
                    if (implName.StartsWith("Winnow"))
                    {
                        return true; // remove all our background jobs including AdminSeeder
                    }
                    return false; // keep Wolverine, Microsoft, etc.
                }
                return false;
            }).ToList();

            foreach (var s in hostedServices) services.Remove(s);

            // Force Caching to use Memory in tests to avoid Redis dependencies
            var cachingDescriptors = services.Where(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache) ||
                d.ServiceType == typeof(Winnow.API.Services.Caching.ICacheService)).ToList();
            foreach (var d in cachingDescriptors) services.Remove(d);

            services.AddDistributedMemoryCache();
            services.AddSingleton<Winnow.API.Services.Caching.ICacheService, Winnow.API.Services.Caching.DistributedCacheService>();

            // Force PoW settings
            var powDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Options.IOptions<Winnow.API.Infrastructure.Security.PoW.PoWSettings>));
            if (powDescriptor != null) services.Remove(powDescriptor);

            services.Configure<Winnow.API.Infrastructure.Security.PoW.PoWSettings>(options =>
            {
                options.Enabled = _enablePoW;
                options.Difficulty = 4;
                options.MaxTimestampAgeMinutes = 5;
            });

            var connString = _connectionString ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                            ?? throw new InvalidOperationException("PostgreSQL connection string not found.");

            // Register test tenant context
            services.AddSingleton<ITenantContext>(new TestTenantContext(_tenantId, connString));
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<Winnow.API.Infrastructure.Security.IApiKeyService>();

        // Create a dummy user for the project owner
        var testUserId = Guid.NewGuid().ToString();
        var testUserEmail = userEmail ?? "test@example.com";
        var testUserPassword = userPassword ?? "Password123!";

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == testUserEmail);
        if (existingUser == null)
        {
            var user = new ApplicationUser
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

        var name = "Test Organization";
        var contactEmail = new Email(testUserEmail);
        var plan = SubscriptionPlan.Free;
        var organization = new Organization(name, contactEmail, plan);
        db.Organizations.Add(organization);

        var ownerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Owner" && r.OrganizationId == null);
        if (ownerRole == null)
        {
            ownerRole = new Winnow.API.Domain.Security.Role("Owner");
            db.Roles.Add(ownerRole);
            await db.SaveChangesAsync();
        }

        var organizationMember = new OrganizationMember(organization.Id, existingUser.Id, ownerRole.Id);
        db.OrganizationMembers.Add(organizationMember);

        var projectId = Guid.NewGuid();
        var generatedApiKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Project(
            organization.Id,
            "Test Project",
            existingUser.Id,
            apiKeyService.HashKey(generatedApiKey),
            projectId);

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (project.Id, generatedApiKey);
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

    /// <summary>
    /// Seeds default roles and permissions for testing.
    /// </summary>
    public async Task SeedDefaultDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        if (!await db.Permissions.AnyAsync())
        {
            var permissions = new[]
            {
                "reports:read", "reports:write", "reports:delete",
                "clusters:read", "clusters:write", "clusters:delete",
                "teams:manage", "billing:manage", "settings:manage",
                "apikeys:manage", "integrations:read", "integrations:manage", "auditlogs:read",
                "projects:read", "projects:manage",
                "organizations:read", "organizations:manage",
                "members:read", "members:manage"
            };

            foreach (var name in permissions)
            {
                db.Permissions.Add(new Winnow.API.Domain.Security.Permission(name));
            }
            await db.SaveChangesAsync();
        }

        // System Roles
        var systemRoles = new[] { "Owner", "Admin", "Member" };
        foreach (var roleName in systemRoles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName && r.OrganizationId == null))
            {
                db.Roles.Add(new Winnow.API.Domain.Security.Role(roleName));
            }
        }
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin" && r.OrganizationId == null);
        var ownerRole = await db.Roles.FirstAsync(r => r.Name == "Owner" && r.OrganizationId == null);
        var memberRole = await db.Roles.FirstAsync(r => r.Name == "Member" && r.OrganizationId == null);

        var allPerms = await db.Permissions.ToListAsync();
        var adminPermNames = new[]
        {
            "reports:read", "reports:write", "clusters:read", "clusters:write",
            "teams:manage", "settings:manage", "integrations:read", "integrations:manage", "auditlogs:read",
            "projects:read", "projects:manage", "organizations:read", "organizations:manage", "members:read", "members:manage"
        };
        var memberPermNames = new[]
        {
            "clusters:read", "reports:read", "projects:read", "organizations:read", "members:read", "integrations:read"
        };

        foreach (var p in allPerms)
        {
            // Owner gets everything
            if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == ownerRole.Id && rp.PermissionId == p.Id))
                db.RolePermissions.Add(new Winnow.API.Domain.Security.RolePermission(ownerRole.Id, p.Id));

            // Admin
            if (adminPermNames.Contains(p.Name))
            {
                if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == p.Id))
                    db.RolePermissions.Add(new Winnow.API.Domain.Security.RolePermission(adminRole.Id, p.Id));
            }

            // Member
            if (memberPermNames.Contains(p.Name))
            {
                if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == memberRole.Id && rp.PermissionId == p.Id))
                    db.RolePermissions.Add(new Winnow.API.Domain.Security.RolePermission(memberRole.Id, p.Id));
            }
        }
        await db.SaveChangesAsync();
    }

    private class TestTenantContext : Winnow.API.Infrastructure.MultiTenancy.TenantContext
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
