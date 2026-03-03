using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Clusters.Merge;

public class MergeClusterRequest
{
    /// <summary>The target cluster ID that others will be merged INTO.</summary>
    public Guid Id { get; set; }

    /// <summary>Source cluster IDs to merge into the target.</summary>
    public List<Guid> SourceIds { get; set; } = new();
}

public sealed class MergeClusterEndpoint(WinnowDbContext db, IClusterService clusterService)
    : Endpoint<MergeClusterRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{Id}/merge");
        Summary(s =>
        {
            s.Summary = "Merge clusters";
            s.Description = "Merges multiple source clusters into a single target cluster. Reports in source clusters are re-assigned and marked as Duplicate. Empty source clusters are deleted.";
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(MergeClusterRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        Guid projectId = Guid.Empty;
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader) ||
            !Guid.TryParse(projectIdHeader, out projectId))
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return; // unreachable but satisfies compiler
        }

        // Validate the target cluster exists and belongs to this project
        var targetCluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == projectId, ct);

        if (targetCluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var sourceIds = req.SourceIds
            .Where(id => id != req.Id)
            .Distinct()
            .ToList();

        if (sourceIds.Count == 0)
        {
            ThrowError("At least one distinct source cluster ID is required.", 400);
        }

        // Re-assign all reports from source clusters to the target cluster
        foreach (var sourceClusterId in sourceIds)
        {
            var sourceCluster = await db.Clusters
                .FirstOrDefaultAsync(c => c.Id == sourceClusterId && c.ProjectId == projectId, ct);

            if (sourceCluster == null) continue;

            var sourceReports = await db.Reports
                .Where(r => r.ClusterId == sourceClusterId && r.ProjectId == projectId)
                .ToListAsync(ct);

            foreach (var report in sourceReports)
            {
                report.ClusterId = targetCluster.Id;
                report.Status = "Duplicate";
            }

            db.Clusters.Remove(sourceCluster);
        }

        await db.SaveChangesAsync(ct);

        // Recalculate centroid for the target cluster with all merged reports
        await clusterService.RecalculateCentroidAsync(targetCluster.Id, ct);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Clusters merged successfully." }, ct);
    }
}
