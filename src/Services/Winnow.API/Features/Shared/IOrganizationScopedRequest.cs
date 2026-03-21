namespace Winnow.API.Features.Shared;

public interface IOrganizationScopedRequest : IOrgScopedRequest
{
    new Guid CurrentOrganizationId { get; set; }
    string CurrentUserId { get; set; }
    HashSet<string> CurrentUserRoles { get; set; }
}

public static class OrganizationScopedRequestExtensions
{
    public static bool HasAnyRole(this IOrganizationScopedRequest request, params string[] allowedRoles)
        => allowedRoles.Any(r => request.CurrentUserRoles.Contains(r));
}
