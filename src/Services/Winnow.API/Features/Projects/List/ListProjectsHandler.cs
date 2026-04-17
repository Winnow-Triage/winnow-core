using Winnow.API.Features.Projects.Dtos;
using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Features.Shared;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Projects.List;

[RequirePermission("projects:read")]
public record ListProjectsQuery : IRequest<List<ProjectDto>>, IOrganizationScopedRequest
{
    public bool OrgWide { get; set; }
    public Guid CurrentOrganizationId { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ListProjectsHandler(WinnowDbContext dbContext) : IRequestHandler<ListProjectsQuery, List<ProjectDto>>
{
    public async Task<List<ProjectDto>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == request.CurrentOrganizationId);

        // Filter by teams if they DIDN'T ask for OrgWide, OR if they lack the required roles.
        bool shouldFilterByTeams = !request.HasAnyRole("Admin", "SuperAdmin", "Owner");

        if (shouldFilterByTeams)
        {
            var userTeamIds = await dbContext.TeamMembers
                .Where(tm => tm.UserId == request.CurrentUserId)
                .Select(tm => tm.TeamId)
                .ToListAsync(cancellationToken);

            var directProjectIds = await dbContext.ProjectMembers
                .Where(pm => pm.UserId == request.CurrentUserId)
                .Select(pm => pm.ProjectId)
                .ToListAsync(cancellationToken);

            query = query.Where(p => p.TeamId == null || userTeamIds.Contains(p.TeamId.Value) || directProjectIds.Contains(p.Id));
        }

        var projects = await query
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                "",
                p.TeamId,
                new Winnow.API.Features.Organizations.Get.NotificationSettingsDto
                {
                    VolumeThreshold = p.Notifications.VolumeThreshold,
                    CriticalityThreshold = p.Notifications.CriticalityThreshold
                },
                !string.IsNullOrEmpty(p.SecondaryApiKeyHash),
                p.SecondaryApiKeyExpiresAt))
            .ToListAsync(cancellationToken);

        return projects;
    }
}
