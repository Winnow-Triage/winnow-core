using FastEndpoints;
using System.Security.Claims;

namespace Winnow.API.Features.Shared;

public abstract class ProjectScopedRequest : OrganizationScopedRequest, IProjectScopedRequest
{
    [FromHeader("X-Project-ID")]
    public Guid CurrentProjectId { get; set; }
}