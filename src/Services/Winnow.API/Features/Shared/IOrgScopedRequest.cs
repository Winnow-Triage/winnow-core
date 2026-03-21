namespace Winnow.API.Features.Shared;

public interface IOrgScopedRequest
{
    Guid CurrentOrganizationId { get; }
}
