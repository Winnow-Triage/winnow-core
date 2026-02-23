using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Factory for creating WinnowDbContext instances during design time (e.g., migrations).
/// This factory is used by EF Core tools when there's no dependency injection available.
/// </summary>
public class WinnowDbContextFactory : IDesignTimeDbContextFactory<WinnowDbContext>
{
    public WinnowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WinnowDbContext>();

        // For design time, use a default tenant context with null organization ID
        // This allows migrations to be generated without requiring tenant-specific logic
        var tenantContext = new DesignTimeTenantContext();

        // Configure SQLite for design time
        optionsBuilder.UseSqlite(tenantContext.ConnectionString);

        // Build configuration for design time
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new WinnowDbContext(optionsBuilder.Options, tenantContext, configuration);
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        public string? TenantId { get; set; }
        public Guid? CurrentOrganizationId { get; set; }
        public string ConnectionString => "Data Source=Data/design-time.db";
    }
}