using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

/// <summary>
/// Request to clear an existing summary.
/// </summary>
public class ClearClusterSummaryRequest : ProjectScopedRequest
{
    public Guid Id { get; set; }
}

public sealed class ClearClusterSummaryEndpoint(WinnowDbContext db) : ProjectScopedEndpoint<ClearClusterSummaryRequest>
{
    public override void Configure()
    {
        Post("/clusters/{Id}/clear-summary");
        Summary(s =>
        {
            s.Summary = "Clear cluster summary";
            s.Description = "Removes the AI-generated summary from a cluster.";
            s.Response(200, "Summary cleared");
            s.Response(404, "Cluster not found");
        });
        Options(x => x.RequireAuthorization());

    }

    public override async Task HandleAsync(ClearClusterSummaryRequest req, CancellationToken ct)
    {
        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.ProjectId == req.CurrentProjectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        cluster.ClearSummary();

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new { }, ct);
    }
}
