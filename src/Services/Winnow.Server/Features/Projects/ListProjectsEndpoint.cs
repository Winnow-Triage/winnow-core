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
/// <param name="HasSecondaryKey">Indicates if there is an active secondary (old) key.</param>
/// <param name="SecondaryApiKeyExpiresAt">When the secondary key will expire, if any.</param>
public record ProjectDto(
    Guid Id,
    string Name,
    string ApiKey,
    Guid? TeamId = null,
    bool HasSecondaryKey = false,
    DateTimeOffset? SecondaryApiKeyExpiresAt = null);

public class ListProjectsRequest
{
    public bool OrgWide { get; set; }
}

public sealed class ListProjectsEndpoint(WinnowDbContext dbContext, ITenantContext tenantContext) : Endpoint<ListProjectsRequest, List<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects");
        Summary(s =>
        {
            s.Summary = "List projects";
            s.Description = "Retrieves a list of projects. If OrgWide is true and user is admin, returns all projects in the organization.";
            s.Response<List<ProjectDto>>(200, "List of projects");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(ListProjectsRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization selected", 400);
            return;
        }

        // Get user role in the organization
        var membership = await dbContext.OrganizationMembers
            .FirstOrDefaultAsync(om => om.UserId == userId && om.OrganizationId == tenantContext.CurrentOrganizationId.Value, ct);

        if (membership == null)
        {
            ThrowError("User is not a member of this organization", 403);
            return;
        }

        var query = dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == tenantContext.CurrentOrganizationId.Value);

        // Access control:
        // - Admins/Owners ALWAYS see all projects in the organization.
        // - Members see projects assigned to their teams OR projects with NO team assigned.
        bool shouldFilterByTeams = !membership.IsAdmin();

        if (shouldFilterByTeams)
        {
            var userTeamIds = await dbContext.TeamMembers
                .Where(tm => tm.UserId == userId && tm.Team!.OrganizationId == tenantContext.CurrentOrganizationId.Value)
                .Select(tm => tm.TeamId)
                .ToListAsync(ct);

            var directProjectIds = await dbContext.ProjectMembers
                .Where(pm => pm.UserId == userId && pm.Project!.OrganizationId == tenantContext.CurrentOrganizationId.Value)
                .Select(pm => pm.ProjectId)
                .ToListAsync(ct);

            query = query.Where(p => p.TeamId == null || userTeamIds.Contains(p.TeamId.Value) || directProjectIds.Contains(p.Id));
        }

        var projects = await query
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                "",
                p.TeamId,
                !string.IsNullOrEmpty(p.SecondaryApiKeyHash),
                p.SecondaryApiKeyExpiresAt))
            .ToListAsync(ct);

        await Send.OkAsync(projects, ct);
    }
}
