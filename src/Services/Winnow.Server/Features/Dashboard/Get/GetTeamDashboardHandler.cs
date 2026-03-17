using Winnow.Server.Features.Dashboard.IService;
using Winnow.Server.Features.Dashboard.Dtos;
using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Dashboard.Get;

[RequirePermission("reports:read")]
public class GetTeamDashboardQuery : IRequest<GetTeamDashboardResult>, IOrganizationScopedRequest
{
    public GetTeamDashboardQuery(Guid currentOrganizationId, Guid teamId, string currentUserId)
    {
        CurrentOrganizationId = currentOrganizationId;
        TeamId = teamId;
        CurrentUserId = currentUserId;
    }

    public Guid CurrentOrganizationId { get; set; }
    public Guid TeamId { get; set; }
    public string CurrentUserId { get; set; }
    public HashSet<string> CurrentUserRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public record GetTeamDashboardResult(bool IsSuccess, TeamDashboardDto? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetTeamDashboardHandler(IDashboardService dashboardService, WinnowDbContext dbContext) : IRequestHandler<GetTeamDashboardQuery, GetTeamDashboardResult>
{
    public async Task<GetTeamDashboardResult> Handle(GetTeamDashboardQuery request, CancellationToken cancellationToken)
    {
        var userHasAccess = await dbContext.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TeamId &&
                           t.OrganizationId == request.CurrentOrganizationId &&
                           dbContext.OrganizationMembers.Any(om => om.OrganizationId == request.CurrentOrganizationId && om.UserId == request.CurrentUserId), cancellationToken);

        if (!userHasAccess)
        {
            return new GetTeamDashboardResult(false, null, "Team not found or access denied", 404);
        }

        var metrics = await dashboardService.GetTeamDashboardAsync(request.CurrentOrganizationId, request.TeamId, cancellationToken);

        return new GetTeamDashboardResult(true, metrics);
    }
}
