namespace Winnow.Server.Infrastructure.MultiTenancy;

public class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
    public Guid? CurrentOrganizationId { get; set; }

    public virtual string ConnectionString => TenantId == null
        ? "Data Source=Data/default.db"
        : $"Data Source=Data/{TenantId}.db";
}
