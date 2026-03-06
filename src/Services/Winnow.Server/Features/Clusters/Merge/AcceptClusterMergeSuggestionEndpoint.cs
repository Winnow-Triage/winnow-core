using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Clusters.Merge;

public class AcceptClusterMergeSuggestionRequest
{
    public Guid Id { get; set; }
}

public sealed class AcceptClusterMergeSuggestionEndpoint(WinnowDbContext db, IClusterService clusterService)
    : Endpoint<AcceptClusterMergeSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/accept-merge-suggestion");
        Summary(s =>
        {
            s.Summary = "Accept a suggested cluster merge";
            s.Description = "Accepts the AI-suggested merge for the specified cluster, moving all reports to the target cluster and marking the source as merged.";
            s.Response<ActionResponse>(200, "Merge accepted");
            s.Response(400, "No pending merge suggestion");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AcceptClusterMergeSuggestionRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        var sourceCluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == projectId, ct);

        if (sourceCluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (sourceCluster.SuggestedMergeClusterId == null)
        {
            ThrowError("No pending merge suggestion for this cluster.");
        }

        var targetClusterId = sourceCluster.SuggestedMergeClusterId.Value;
        var targetCluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == targetClusterId && c.ProjectId == projectId, ct);

        if (targetCluster == null)
        {
            ThrowError("The suggested target cluster no longer exists.");
        }

        // Move all reports from source to target
        var sourceReports = await db.Reports
            .Where(r => r.ClusterId == sourceCluster.Id && r.ProjectId == projectId)
            .ToListAsync(ct);

        foreach (var report in sourceReports)
        {
            report.ClusterId = targetCluster.Id;
            report.Status = "Duplicate";
        }

        // Clear any suggestion references pointing to the source cluster before deleting it,
        // to avoid orphaned SuggestedClusterId values inflating the pending decisions count.
        await db.Reports
            .Where(r => r.ProjectId == projectId && r.SuggestedClusterId == sourceCluster.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.SuggestedClusterId, (Guid?)null)
                .SetProperty(r => r.SuggestedConfidenceScore, (float?)null), ct);

        await db.Clusters
            .Where(c => c.ProjectId == projectId && c.SuggestedMergeClusterId == sourceCluster.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.SuggestedMergeClusterId, (Guid?)null)
                .SetProperty(c => c.SuggestedMergeConfidenceScore, (float?)null), ct);

        // Delete source cluster
        db.Clusters.Remove(sourceCluster);
        await db.SaveChangesAsync(ct);

        // Recalculate centroid for the target cluster
        await clusterService.RecalculateCentroidAsync(targetCluster.Id, ct);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Cluster merge accepted successfully." }, ct);
    }
}
