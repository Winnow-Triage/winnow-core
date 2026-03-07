using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Close;

/// <summary>
/// Request to close a cluster of reports.
/// </summary>
public class CloseClusterRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the cluster to close.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class CloseClusterEndpoint(WinnowDbContext db) : ProjectScopedEndpoint<CloseClusterRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/clusters/{id}/close-cluster");
        Summary(s =>
        {
            s.Summary = "Close a cluster of reports";
            s.Description = "Closes all reports in the specified cluster.";
            s.Response<ActionResponse>(200, "Cluster closed successfully");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CloseClusterRequest req, CancellationToken ct)
    {
        var cluster = await db.Clusters.FindAsync([req.Id], ct);

        if (cluster == null || cluster.ProjectId != req.CurrentProjectId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var clusterId = cluster.Id;

        // Close all reports in the cluster
        var clusterReports = await db.Reports
            .Where(t => t.ProjectId == req.CurrentProjectId && t.ClusterId == clusterId)
            .ToListAsync(ct);

        foreach (var t in clusterReports)
        {
            // We should use the same mapped state. Let's use Dismissed.
            t.ChangeStatus(ReportStatus.Dismissed);
        }

        // Close the cluster itself
        cluster.ChangeStatus(ClusterStatus.Dismissed);

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = $"Closed {clusterReports.Count} reports in cluster." }, ct);
    }
}
