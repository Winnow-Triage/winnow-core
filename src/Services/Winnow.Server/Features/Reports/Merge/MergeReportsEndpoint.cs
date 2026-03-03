using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.Merge;

/// <summary>
/// Request to merge multiple reports into a target report's cluster.
/// </summary>
public class MergeReportsRequest
{
    /// <summary>
    /// The target report ID that others will be merged INTO.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// List of source report IDs to merge into the target.
    /// </summary>
    public List<Guid> SourceIds { get; set; } = new();
}

public sealed class MergeReportsEndpoint(WinnowDbContext db, IClusterService clusterService) : Endpoint<MergeReportsRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/merge");
        Summary(s =>
        {
            s.Summary = "Merge reports into a cluster";
            s.Description = "Merges multiple source reports into the cluster of a single target report. Source reports are marked as Duplicate.";
            s.Response<ActionResponse>(200, "Reports merged successfully");
            s.Response(404, "Target report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(MergeReportsRequest req, CancellationToken ct)
    {
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader) || !Guid.TryParse(projectIdHeader, out Guid projectId))
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return;
        }

        // Validate user owns this project
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
            return;
        }

        var targetReport = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == projectId, ct);
        if (targetReport == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Ensure target has a cluster
        if (targetReport.ClusterId == null)
        {
            var newCluster = new Cluster
            {
                ProjectId = projectId,
                OrganizationId = targetReport.OrganizationId,
                Centroid = targetReport.Embedding,
                Title = targetReport.Title,
                Status = "Open",
            };
            db.Clusters.Add(newCluster);
            targetReport.ClusterId = newCluster.Id;
        }

        var targetClusterId = targetReport.ClusterId!.Value;
        var clustersToDelete = new HashSet<Guid>();

        foreach (var sourceId in req.SourceIds)
        {
            if (sourceId == req.Id) continue;

            var sourceReport = await db.Reports
                .FirstOrDefaultAsync(r => r.Id == sourceId && r.ProjectId == projectId, ct);
            if (sourceReport == null) continue;

            // If source has its own cluster, move all its members to target cluster
            if (sourceReport.ClusterId != null && sourceReport.ClusterId != targetClusterId)
            {
                var sourceClusterId = sourceReport.ClusterId.Value;
                var children = await db.Reports
                    .Where(t => t.ProjectId == projectId && t.ClusterId == sourceClusterId)
                    .ToListAsync(ct);

                foreach (var child in children)
                {
                    child.ClusterId = targetClusterId;
                    if (child.Id != targetReport.Id)
                    {
                        child.Status = "Duplicate";
                    }
                }

                clustersToDelete.Add(sourceClusterId);
            }
            else
            {
                sourceReport.ClusterId = targetClusterId;
                sourceReport.Status = "Duplicate";
            }
        }

        // Delete empty source clusters
        foreach (var cid in clustersToDelete)
        {
            var cluster = await db.Clusters.FindAsync([cid], ct);
            if (cluster != null)
            {
                db.Clusters.Remove(cluster);
            }
        }

        await db.SaveChangesAsync(ct);

        // Recalculate centroid for the target cluster
        if (targetClusterId != Guid.Empty)
        {
            await clusterService.RecalculateCentroidAsync(targetClusterId, ct);
            await db.SaveChangesAsync(ct);
        }

        await Send.OkAsync(new ActionResponse { Message = "Reports merged successfully." }, ct);
    }
}
