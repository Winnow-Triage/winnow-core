using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.List;

public class ListClustersRequest : ProjectScopedRequest { }

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

public sealed class ListClustersEndpoint(WinnowDbContext dbContext) : ProjectScopedEndpoint<ListClustersRequest, List<ClusterDto>>
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

    public override async Task HandleAsync(ListClustersRequest req, CancellationToken ct)
    {
        // Sort parameter
        string sort = HttpContext.Request.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "criticality";

        var query = dbContext.Clusters
            .AsNoTracking()
            .Where(c => c.ProjectId == req.CurrentProjectId);

        var clusters = await query
            .Select(c => new ClusterDto(
                c.Id,
                c.Title,
                c.Summary,
                c.CriticalityScore,
                c.Status.Name,
                c.CreatedAt,
                dbContext.Reports.Count(r => r.ClusterId == c.Id),
                dbContext.Reports.Any(r => r.ClusterId == c.Id && r.IsLocked),
                dbContext.Reports.Any(r => r.ClusterId == c.Id && r.IsOverage)))
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
