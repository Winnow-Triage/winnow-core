using FastEndpoints;
using System.Security.Claims;

namespace Winnow.Server.Features.Shared;

public abstract class ProjectScopedRequest : OrganizationScopedRequest, IProjectScopedRequest
{
    [FromHeader("X-Project-ID")]
    public Guid CurrentProjectId { get; set; }
}