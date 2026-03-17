using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;
using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.Merge;

[RequirePermission("clusters:write")]
public record AcceptClusterMergeSuggestionCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<AcceptClusterMergeSuggestionResult>, IOrgScopedRequest;

public record AcceptClusterMergeSuggestionResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class AcceptClusterMergeSuggestionHandler(WinnowDbContext db, IClusterService clusterService) : IRequestHandler<AcceptClusterMergeSuggestionCommand, AcceptClusterMergeSuggestionResult>
{
    public async Task<AcceptClusterMergeSuggestionResult> Handle(AcceptClusterMergeSuggestionCommand request, CancellationToken cancellationToken)
    {
        var sourceCluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);

        if (sourceCluster == null)
        {
            return new AcceptClusterMergeSuggestionResult(false, "Cluster not found", 404);
        }

        if (sourceCluster.SuggestedMergeClusterId == null)
        {
            return new AcceptClusterMergeSuggestionResult(false, "No pending merge suggestion for this cluster.", 400);
        }

        var targetClusterId = sourceCluster.SuggestedMergeClusterId.Value;
        var targetCluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == targetClusterId && c.ProjectId == request.ProjectId, cancellationToken);

        if (targetCluster == null)
        {
            return new AcceptClusterMergeSuggestionResult(false, "The suggested target cluster no longer exists.", 400);
        }

        var sourceReports = await db.Reports
            .Where(r => r.ClusterId == sourceCluster.Id && r.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        foreach (var report in sourceReports)
        {
            report.AssignToCluster(targetCluster.Id);
            report.ChangeStatus(ReportStatus.Duplicate);
            report.ClearSuggestedCluster();
        }

        await db.Clusters
            .Where(c => c.ProjectId == request.ProjectId && c.SuggestedMergeClusterId == sourceCluster.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.SuggestedMergeClusterId, (Guid?)null)
                .SetProperty(c => c.SuggestedMergeConfidenceScore, (ConfidenceScore?)null), cancellationToken);

        db.Clusters.Remove(sourceCluster);
        await db.SaveChangesAsync(cancellationToken);

        await clusterService.RecalculateCentroidAsync(targetCluster.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new AcceptClusterMergeSuggestionResult(true);
    }
}
