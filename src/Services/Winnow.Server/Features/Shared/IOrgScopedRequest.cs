namespace Winnow.Server.Features.Shared;

public interface IOrgScopedRequest
{
    Guid CurrentOrganizationId { get; }
}
