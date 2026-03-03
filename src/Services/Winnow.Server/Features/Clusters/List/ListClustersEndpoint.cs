using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.List;

/// <summary>
/// Summary of a cluster.
/// </summary>
public record ClusterDto(
    Guid Id,
    string? Title,
    string? Summary,
    int? CriticalityScore,
    string Status,
    DateTime CreatedAt,
    int ReportCount,
    bool IsLocked,
    bool IsOverage);

public sealed class ListClustersEndpoint(WinnowDbContext dbContext) : EndpointWithoutRequest<List<ClusterDto>>
{
    public override void Configure()
    {
        Get("/clusters");
        Summary(s =>
        {
            s.Summary = "List active clusters";
            s.Description = "Retrieves a list of report clusters for the project, including AI summaries and criticality.";
            s.Response<List<ClusterDto>>(200, "List of clusters");
            s.Response(400, "Invalid request");
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

        string sort = HttpContext.Request.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "criticality";

        var query = dbContext.Clusters
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId);

        var clusters = await query
            .Select(c => new ClusterDto(
                c.Id,
                c.Title,
                c.Summary,
                c.CriticalityScore,
                c.Status,
                c.CreatedAt,
                c.Reports.Count,
                c.Reports.Any(r => r.IsLocked),
                c.Reports.Any(r => r.IsOverage)))
            .ToListAsync(ct);

        // Perform sorting in memory for simplicity if complex, but here we can do it
        var sortedClusters = sort switch
        {
            "criticality" => clusters.OrderByDescending(c => c.CriticalityScore ?? 0).ThenByDescending(c => c.ReportCount),
            "newest" => clusters.OrderByDescending(c => c.CreatedAt),
            _ => clusters.OrderByDescending(c => c.ReportCount)
        };

        await Send.OkAsync(sortedClusters.ToList(), ct);
    }
}
