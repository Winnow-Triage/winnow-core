using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

/// <summary>
/// Project details.
/// </summary>
/// <param name="Id">Unique identifier of the project.</param>
/// <param name="Name">Name of the project.</param>
/// <param name="ApiKey">API Key key for the project.</param>
/// <param name="TeamId">The ID of the team this project belongs to, if any.</param>
public record ProjectDto(Guid Id, string Name, string ApiKey, Guid? TeamId = null);

public sealed class ListProjectsEndpoint(WinnowDbContext dbContext, ITenantContext tenantContext) : EndpointWithoutRequest<List<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects");
        Summary(s =>
        {
            s.Summary = "List user projects";
            s.Description = "Retrieves a list of all projects owned by the authenticated user.";
            s.Response<List<ProjectDto>>(200, "List of projects");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
        // We will enable strict auth in Program.cs, but we can also enforce it here
        // Claims: assuming standard JWT claims
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);


        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
            return;
        }

        var projects = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.OrganizationId == tenantContext.CurrentOrganizationId.Value)
            .Select(p => new ProjectDto(p.Id, p.Name, "", p.TeamId))
            .ToListAsync(ct);

        await Send.OkAsync(projects, ct);
    }
}
