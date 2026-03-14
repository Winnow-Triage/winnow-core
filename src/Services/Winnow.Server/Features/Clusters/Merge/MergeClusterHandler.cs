using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Clusters.Merge;

public record MergeClusterCommand(Guid Id, Guid ProjectId, List<Guid> SourceIds) : IRequest<MergeClusterResult>;

public record MergeClusterResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class MergeClusterHandler(WinnowDbContext db, IClusterService clusterService) : IRequestHandler<MergeClusterCommand, MergeClusterResult>
{
    public async Task<MergeClusterResult> Handle(MergeClusterCommand request, CancellationToken cancellationToken)
    {
        var targetCluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);

        if (targetCluster == null)
        {
            return new MergeClusterResult(false, "Cluster not found", 404);
        }

        var sourceIds = request.SourceIds
            .Where(id => id != request.Id)
            .Distinct()
            .ToList();

        if (sourceIds.Count == 0)
        {
            return new MergeClusterResult(false, "At least one distinct source cluster ID is required.", 400);
        }

        foreach (var sourceClusterId in sourceIds)
        {
            var sourceCluster = await db.Clusters
                .FirstOrDefaultAsync(c => c.Id == sourceClusterId && c.ProjectId == request.ProjectId, cancellationToken);

            if (sourceCluster == null) continue;

            var sourceReports = await db.Reports
                .Where(r => r.ClusterId == sourceClusterId && r.ProjectId == request.ProjectId)
                .ToListAsync(cancellationToken);

            foreach (var report in sourceReports)
            {
                report.AssignToCluster(targetCluster.Id);
                report.ChangeStatus(ReportStatus.Dismissed);
            }

            db.Clusters.Remove(sourceCluster);
        }

        await db.SaveChangesAsync(cancellationToken);

        await clusterService.RecalculateCentroidAsync(targetCluster.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new MergeClusterResult(true);
    }
}
