using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.Close;

[RequirePermission("reports:write")]
public record CloseClusterCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<CloseClusterResult>, IOrgScopedRequest;

public record CloseClusterResult(bool IsSuccess, string? Message = null, string? ErrorMessage = null, int? StatusCode = null);

public class CloseClusterHandler(WinnowDbContext db) : IRequestHandler<CloseClusterCommand, CloseClusterResult>
{
    public async Task<CloseClusterResult> Handle(CloseClusterCommand request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters.FindAsync([request.Id], cancellationToken);

        if (cluster == null || cluster.ProjectId != request.ProjectId || cluster.OrganizationId != request.CurrentOrganizationId)
        {
            return new CloseClusterResult(false, null, "Cluster not found", 404);
        }

        var clusterId = cluster.Id;

        // Close all reports in the cluster
        var clusterReports = await db.Reports
            .Where(t => t.ProjectId == request.ProjectId && t.ClusterId == clusterId)
            .ToListAsync(cancellationToken);

        foreach (var t in clusterReports)
        {
            // We should use the same mapped state. Let's use Dismissed.
            t.ChangeStatus(ReportStatus.Dismissed);
        }

        // Close the cluster itself
        cluster.ChangeStatus(ClusterStatus.Dismissed);

        await db.SaveChangesAsync(cancellationToken);
        return new CloseClusterResult(true, $"Closed {clusterReports.Count} reports in cluster.");
    }
}
