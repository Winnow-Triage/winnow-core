using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Merge;

public class MergeClustersRequest
{
    public Guid Id { get; set; } // Target Cluster (Parent)
    public List<Guid> SourceIds { get; set; } = new(); // Source Clusters/Reports to merge INTO target
}

public class MergeClustersEndpoint(WinnowDbContext db) : Endpoint<MergeClustersRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/merge");
        AllowAnonymous();
    }

    public override async Task HandleAsync(MergeClustersRequest req, CancellationToken ct)
    {
        var targetReport = await db.Reports.FindAsync([req.Id], ct);
        if (targetReport == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        foreach (var sourceId in req.SourceIds)
        {
            if (sourceId == req.Id) continue;

            var sourceReport = await db.Reports.FindAsync([sourceId], ct);
            if (sourceReport == null) continue;

            sourceReport.ParentReportId = targetReport.Id;
            sourceReport.Status = "Duplicate";

            var children = await db.Reports
                .Where(t => t.ParentReportId == sourceId)
                .ToListAsync(ct);

            foreach (var child in children)
            {
                child.ParentReportId = targetReport.Id;
            }
        }

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Clusters merged successfully." }, ct);
    }
}
