namespace Winnow.API.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    string? TenantId { get; set; }
    Guid? CurrentOrganizationId { get; set; }
    string ConnectionString { get; }
}
