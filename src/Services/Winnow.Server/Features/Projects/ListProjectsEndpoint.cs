using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Projects;

public record ProjectDto(
    Guid Id,
    string Name,
    string ApiKey,
    Guid? TeamId = null,
    bool HasSecondaryKey = false,
    DateTimeOffset? SecondaryApiKeyExpiresAt = null);

public class ListProjectsRequest : OrganizationScopedRequest
{
    public bool OrgWide { get; set; }
}

public sealed class ListProjectsEndpoint(WinnowDbContext dbContext)
    : OrganizationScopedEndpoint<ListProjectsRequest, List<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects");
        Summary(s =>
        {
            s.Summary = "List projects";
            s.Description = "Retrieves a list of projects. If OrgWide is true and user is admin, returns all projects in the organization.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(ListProjectsRequest req, CancellationToken ct)
    {
        var query = dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == req.CurrentOrganizationId);

        Console.WriteLine($"ROLES LOADED: {string.Join(", ", req.CurrentUserRoles)}");
        // Filter by teams if they DIDN'T ask for OrgWide, OR if they lack the required roles.
        bool shouldFilterByTeams = !req.HasAnyRole("Admin", "SuperAdmin", "Owner");

        if (shouldFilterByTeams)
        {
            var userTeamIds = await dbContext.TeamMembers
                .Where(tm => tm.UserId == req.CurrentUserId && tm.Team!.OrganizationId == req.CurrentOrganizationId)
                .Select(tm => tm.TeamId)
                .ToListAsync(ct);

            var directProjectIds = await dbContext.ProjectMembers
                .Where(pm => pm.UserId == req.CurrentUserId && pm.Project!.OrganizationId == req.CurrentOrganizationId)
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