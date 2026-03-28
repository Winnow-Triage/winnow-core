using FastEndpoints;
using System.Security.Claims;

namespace Winnow.API.Features.Shared;

public abstract class OrganizationScopedRequest : IOrganizationScopedRequest
{
    public Guid CurrentOrganizationId { get; set; }

    [FromClaim(ClaimTypes.NameIdentifier)]
    public string CurrentUserId { get; set; } = string.Empty;

    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasAnyRole(params string[] allowedRoles)
        => allowedRoles.Any(r => CurrentUserRoles.Contains(r));
}