using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard;

public sealed class GetDashboardMetricsEndpoint(IDashboardService dashboardService, WinnowDbContext dbContext) : EndpointWithoutRequest<DashboardMetricsDto>
{
    public override void Configure()
    {
        Get("/dashboard/metrics");
        Summary(s =>
        {
            s.Summary = "Get dashboard metrics";
            s.Description = "Retrieves aggregated metrics for the project dashboard (e.g., usage, error rates).";
            s.Response<DashboardMetricsDto>(200, "Metrics data");
            s.Response(400, "Invalid project ID");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Get user ID and organization ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdClaim = User.FindFirstValue("organization");
        
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            ThrowError("Organization ID not found in token", 401);
        }

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        // Validate user has access to this project in the organization
        var userHasAccess = await dbContext.Projects
            .Include(p => p.Organization!)
            .ThenInclude(o => o.Members)
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && 
                         p.OrganizationId == organizationId &&
                         p.Organization!.Members.Any(m => m.UserId == userId), ct);

        if (!userHasAccess)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var metrics = await dashboardService.GetDashboardMetricsAsync(projectId, organizationId, ct);
        await Send.OkAsync(metrics, ct);
    }
}
