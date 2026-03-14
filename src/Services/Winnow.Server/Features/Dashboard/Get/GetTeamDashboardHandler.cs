using Winnow.Server.Features.Dashboard.IService;
using Winnow.Server.Features.Dashboard.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard.Get;

public record GetTeamDashboardQuery(Guid OrganizationId, Guid TeamId, Guid UserId) : IRequest<GetTeamDashboardResult>;

public record GetTeamDashboardResult(bool IsSuccess, TeamDashboardDto? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetTeamDashboardHandler(IDashboardService dashboardService, WinnowDbContext dbContext) : IRequestHandler<GetTeamDashboardQuery, GetTeamDashboardResult>
{
    public async Task<GetTeamDashboardResult> Handle(GetTeamDashboardQuery request, CancellationToken cancellationToken)
    {
        var userHasAccess = await dbContext.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TeamId &&
                           t.OrganizationId == request.OrganizationId &&
                           dbContext.OrganizationMembers.Any(om => om.OrganizationId == request.OrganizationId && om.UserId == request.UserId.ToString()), cancellationToken);

        if (!userHasAccess)
        {
            return new GetTeamDashboardResult(false, null, "Team not found or access denied", 404);
        }

        var metrics = await dashboardService.GetTeamDashboardAsync(request.OrganizationId, request.TeamId, cancellationToken);

        return new GetTeamDashboardResult(true, metrics);
    }
}
