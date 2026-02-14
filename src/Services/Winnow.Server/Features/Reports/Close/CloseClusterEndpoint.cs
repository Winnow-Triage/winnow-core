using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Close;

public class CloseClusterRequest
{
    public Guid Id { get; set; }
}

public class CloseClusterEndpoint(WinnowDbContext db) : Endpoint<CloseClusterRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/close-cluster");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CloseClusterRequest req, CancellationToken ct)
    {
        var report = await db.Reports.FindAsync([req.Id], ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Determine the Cluster ID (Parent ID)
        var clusterId = report.ParentReportId ?? report.Id;

        // Find all reports in this cluster (Parent + Children)
        var clusterReports = await db.Reports
            .Where(t => t.Id == clusterId || t.ParentReportId == clusterId)
            .ToListAsync(ct);

        foreach (var t in clusterReports)
        {
            t.Status = "Closed";
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = $"Closed {clusterReports.Count} reports in cluster." }, ct);
    }
}
