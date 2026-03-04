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

        // Build configuration for design time
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // For design time, use a default tenant context with null organization ID
        var tenantContext = new DesignTimeTenantContext(configuration);

        var connString = configuration.GetConnectionString("Postgres")
                ?? "Host=localhost;Port=5432;Database=winnow_dev;Username=winnow;Password=winnow_dev";

        optionsBuilder.UseNpgsql(connString,
            npgsql =>
            {
                npgsql.UseVector();
                npgsql.MigrationsAssembly("Winnow.Server");
            });

        return new WinnowDbContext(optionsBuilder.Options, tenantContext, configuration);
    }

    private class DesignTimeTenantContext(IConfiguration configuration) : ITenantContext
    {
        private readonly IConfiguration _configuration = configuration;

        public string? TenantId { get; set; }
        public Guid? CurrentOrganizationId { get; set; }

        public string ConnectionString =>
            _configuration.GetConnectionString("Postgres") ?? "Host=localhost;Port=5432;Database=winnow_dev;Username=winnow;Password=winnow_dev";
    }
}