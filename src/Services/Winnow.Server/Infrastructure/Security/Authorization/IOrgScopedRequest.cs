namespace Winnow.Server.Infrastructure.Security.Authorization;

public interface IOrgScopedRequest
{
    Guid OrgId { get; }
}
