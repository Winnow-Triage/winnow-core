using Microsoft.Extensions.Configuration;

namespace Winnow.API.Infrastructure.MultiTenancy;

public class TenantContext(IConfiguration configuration) : ITenantContext
{
    private readonly IConfiguration _configuration = configuration;

    public string? TenantId { get; set; }
    public Guid? CurrentOrganizationId { get; set; }

    public virtual string ConnectionString =>
        _configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("PostgreSQL connection string 'Postgres' is missing.");
}
