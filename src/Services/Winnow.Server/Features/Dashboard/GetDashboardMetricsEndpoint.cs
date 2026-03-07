using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard;


public class GetDashboardMetricsRequest
{
    [FromHeader("X-Project-ID", IsRequired = true)]
    public Guid ProjectId { get; set; }
}

public sealed class GetDashboardMetricsEndpoint(
    IDashboardService dashboardService,
    WinnowDbContext dbContext,
    ITenantContext tenantContext)
    : Endpoint<GetDashboardMetricsRequest, DashboardMetricsDto>
{
    public override void Configure()
    {
        Get("/dashboard/metrics");
        Summary(s =>
        {
            s.Summary = "Get dashboard metrics";
            s.Description = "Retrieves aggregated metrics for the project dashboard.";
            s.Response<DashboardMetricsDto>(200, "Metrics data");
            s.Response(400, "Invalid project ID");
            s.Response(404, "Project not found or access denied");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetDashboardMetricsRequest req, CancellationToken ct)
    {
        // 3. Use your TenantContext to verify auth instead of manual claim parsing
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        var projectExists = await dbContext.Projects
            .AnyAsync(p => p.Id == req.ProjectId && p.OrganizationId == orgId, ct);

        if (!projectExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Pass the clean, validated IDs to your service
        var metrics = await dashboardService.GetDashboardMetricsAsync(orgId, req.ProjectId, null, ct);

        await Send.OkAsync(metrics, ct);
    }
}