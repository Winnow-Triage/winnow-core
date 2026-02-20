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
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
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

        // Validate user owns this project
        var userOwnsProject = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var metrics = await dashboardService.GetDashboardMetricsAsync(projectId, ct);
        await Send.OkAsync(metrics, ct);
    }
}
