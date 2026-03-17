namespace Winnow.Server.Features.Shared;

public interface IOrgScopedRequest
{
    Guid OrgId { get; }
}
