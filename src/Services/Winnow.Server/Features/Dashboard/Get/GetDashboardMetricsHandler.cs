using Winnow.Server.Features.Dashboard.IService;
using Winnow.Server.Features.Dashboard.Dtos;
using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Dashboard.Get;

[RequirePermission("reports:read")]
public class GetDashboardMetricsQuery(Guid currentOrganizationId, Guid projectId) : IRequest<GetDashboardMetricsResult>, IOrgScopedRequest
{
    public Guid CurrentOrganizationId { get; set; } = currentOrganizationId;
    public Guid ProjectId { get; set; } = projectId;
}

public record GetDashboardMetricsResult(bool IsSuccess, DashboardMetricsDto? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetDashboardMetricsHandler(IDashboardService dashboardService, WinnowDbContext dbContext) : IRequestHandler<GetDashboardMetricsQuery, GetDashboardMetricsResult>
{
    public async Task<GetDashboardMetricsResult> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == request.ProjectId && p.OrganizationId == request.CurrentOrganizationId, cancellationToken);

        if (!projectExists)
        {
            return new GetDashboardMetricsResult(false, null, "Project not found or access denied", 404);
        }

        var metrics = await dashboardService.GetDashboardMetricsAsync(request.CurrentOrganizationId, request.ProjectId, null, cancellationToken);

        return new GetDashboardMetricsResult(true, metrics);
    }
}
