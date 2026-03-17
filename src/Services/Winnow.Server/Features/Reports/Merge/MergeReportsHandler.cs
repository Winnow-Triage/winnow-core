using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.Merge;

[RequirePermission("reports:write")]
public record MergeReportsCommand(Guid OrgId, Guid TargetId, Guid ProjectId, List<Guid> SourceIds) : IRequest<MergeReportsResult>, IOrgScopedRequest;

public record MergeReportsResult(bool IsSuccess, string? Message = null, string? ErrorMessage = null, int? StatusCode = null);

public class MergeReportsHandler(WinnowDbContext db, IClusterService clusterService) : IRequestHandler<MergeReportsCommand, MergeReportsResult>
{
    public async Task<MergeReportsResult> Handle(MergeReportsCommand request, CancellationToken cancellationToken)
    {
        var targetReport = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == request.TargetId && r.ProjectId == request.ProjectId, cancellationToken);

        if (targetReport == null)
        {
            return new MergeReportsResult(false, null, "Target report not found", 404);
        }

        // Ensure target has a cluster
        if (targetReport.ClusterId == null)
        {
            var newCluster = new Cluster(request.ProjectId, targetReport.OrganizationId, targetReport.Id);
            if (targetReport.Embedding != null)
            {
                newCluster.UpdateCentroid(targetReport.Embedding);
            }
            db.Clusters.Add(newCluster);
            targetReport.AssignToCluster(newCluster.Id);
        }

        var targetClusterId = targetReport.ClusterId!.Value;
        var clustersToDelete = new HashSet<Guid>();

        foreach (var sourceId in request.SourceIds)
        {
            if (sourceId == request.TargetId) continue;

            var sourceReport = await db.Reports
                .FirstOrDefaultAsync(r => r.Id == sourceId && r.ProjectId == request.ProjectId, cancellationToken);

            if (sourceReport == null) continue;

            // If source has its own cluster, move all its members to target cluster
            if (sourceReport.ClusterId != null && sourceReport.ClusterId != targetClusterId)
            {
                var sourceClusterId = sourceReport.ClusterId.Value;
                var children = await db.Reports
                    .Where(t => t.ProjectId == request.ProjectId && t.ClusterId == sourceClusterId)
                    .ToListAsync(cancellationToken);

                foreach (var child in children)
                {
                    child.AssignToCluster(targetClusterId);
                    if (child.Id != targetReport.Id)
                    {
                        child.ChangeStatus(ReportStatus.Dismissed);
                    }
                }

                clustersToDelete.Add(sourceClusterId);
            }
            else
            {
                sourceReport.AssignToCluster(targetClusterId);
                sourceReport.ChangeStatus(ReportStatus.Dismissed);
            }
        }

        // Delete empty source clusters
        foreach (var cid in clustersToDelete)
        {
            var cluster = await db.Clusters.FindAsync([cid], cancellationToken);
            if (cluster != null)
            {
                db.Clusters.Remove(cluster);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Recalculate centroid for the target cluster
        if (targetClusterId != Guid.Empty)
        {
            await clusterService.RecalculateCentroidAsync(targetClusterId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new MergeReportsResult(true, "Reports merged successfully.");
    }
}
