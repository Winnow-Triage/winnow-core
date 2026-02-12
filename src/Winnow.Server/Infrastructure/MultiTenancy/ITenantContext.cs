namespace Winnow.Server.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    string? TenantId { get; }
    string ConnectionString { get; }
}
