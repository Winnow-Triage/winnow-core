using FastEndpoints;
using System.Security.Claims;

namespace Winnow.Server.Features.Shared;

public abstract class ProjectScopedRequest : OrganizationScopedRequest
{
    [FromHeader("X-Project-ID")]
    public Guid CurrentProjectId { get; set; }
}