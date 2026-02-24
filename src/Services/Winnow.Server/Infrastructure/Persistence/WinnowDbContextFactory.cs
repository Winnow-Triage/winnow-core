using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Infrastructure.Persistence;

/// <summary>
/// Factory for creating WinnowDbContext instances during design time (e.g., migrations).
/// This factory is used by EF Core tools when there's no dependency injection available.
/// It reads DatabaseProvider from configuration to generate migrations for the correct provider.
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
        // This allows migrations to be generated without requiring tenant-specific logic
        var tenantContext = new DesignTimeTenantContext(configuration);

        // Configure the correct provider based on DatabaseProvider setting
        var provider = configuration["DatabaseProvider"] ?? "Sqlite";

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connString = configuration.GetConnectionString("Postgres")
                ?? "Host=localhost;Port=5432;Database=winnow_dev;Username=winnow;Password=winnow_dev";
            optionsBuilder.UseNpgsql(connString,
                npgsql => npgsql.MigrationsAssembly("Winnow.Server"));
        }
        else
        {
            optionsBuilder.UseSqlite(tenantContext.ConnectionString,
                sqlite => sqlite.MigrationsAssembly("Winnow.Server"));
        }

        optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsAssembly, ProviderMigrationsAssembly>();

        return new WinnowDbContext(optionsBuilder.Options, tenantContext, configuration);
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        private readonly IConfiguration _configuration;

        public DesignTimeTenantContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string? TenantId { get; set; }
        public Guid? CurrentOrganizationId { get; set; }

        public string ConnectionString =>
            _configuration.GetConnectionString("Sqlite") ?? "Data Source=Data/design-time.db";
    }
}