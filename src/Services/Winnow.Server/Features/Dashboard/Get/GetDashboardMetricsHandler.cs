using Winnow.Server.Features.Dashboard.IService;
using Winnow.Server.Features.Dashboard.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard.Get;

public record GetDashboardMetricsQuery(Guid OrganizationId, Guid ProjectId) : IRequest<GetDashboardMetricsResult>;

public record GetDashboardMetricsResult(bool IsSuccess, DashboardMetricsDto? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetDashboardMetricsHandler(IDashboardService dashboardService, WinnowDbContext dbContext) : IRequestHandler<GetDashboardMetricsQuery, GetDashboardMetricsResult>
{
    public async Task<GetDashboardMetricsResult> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == request.ProjectId && p.OrganizationId == request.OrganizationId, cancellationToken);

        if (!projectExists)
        {
            return new GetDashboardMetricsResult(false, null, "Project not found or access denied", 404);
        }

        var metrics = await dashboardService.GetDashboardMetricsAsync(request.OrganizationId, request.ProjectId, null, cancellationToken);

        return new GetDashboardMetricsResult(true, metrics);
    }
}
