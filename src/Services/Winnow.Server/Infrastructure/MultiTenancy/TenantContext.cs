using Microsoft.Extensions.Configuration;

namespace Winnow.Server.Infrastructure.MultiTenancy;

public class TenantContext : ITenantContext
{
    private readonly IConfiguration _configuration;

    public TenantContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? TenantId { get; set; }
    public Guid? CurrentOrganizationId { get; set; }

    public virtual string ConnectionString
    {
        get
        {
            var provider = _configuration["DatabaseProvider"] ?? "Sqlite";

            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                // For Postgres, all tenants share a single database with row-level isolation.
                // The connection string comes from configuration, not from TenantId.
                return _configuration.GetConnectionString("Postgres")
                    ?? "Host=localhost;Port=5432;Database=winnow_dev;Username=winnow;Password=winnow_dev";
            }

            // SQLite: each tenant gets its own database file
            return TenantId == null
                ? _configuration.GetConnectionString("Sqlite") ?? "Data Source=Data/default.db"
                : $"Data Source=Data/{TenantId}.db";
        }
    }
}
