using FastEndpoints;
using System.Security.Claims;

namespace Winnow.API.Features.Shared;

public abstract class ProjectScopedRequest : OrganizationScopedRequest, IProjectScopedRequest
{
    /// <summary>
    /// The Project ID from the X-Project-ID header. Used for session/contextual scoping.
    /// </summary>
    [FromHeader("X-Project-ID", isRequired: false)]
    public Guid CurrentProjectId { get; set; }

    /// <summary>
    /// The Project ID from the route path (e.g. /projects/{ProjectId}).
    /// If provided, this usually identifies the target resource for management operations.
    /// </summary>
    public Guid ProjectId { get; set; }
}