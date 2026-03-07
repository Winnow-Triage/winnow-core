using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.SuggestActions;

/// <summary>
/// Request to accept a suggested cluster assignment.
/// </summary>
public class AcceptSuggestionRequest : ProjectScopedRequest
{
    /// <summary>
    /// ID of the report to accept a suggestion for.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class AcceptSuggestionEndpoint(WinnowDbContext db, IClusterService clusterService) : ProjectScopedEndpoint<AcceptSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/accept-suggestion");
        Summary(s =>
        {
            s.Summary = "Accept a suggested cluster assignment";
            s.Description = "Accepts the AI-suggested cluster for the specified report, assigning it to the cluster.";
            s.Response<ActionResponse>(200, "Suggestion accepted");
            s.Response(400, "No pending suggestion");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(AcceptSuggestionRequest req, CancellationToken ct)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == req.CurrentProjectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (report.SuggestedClusterId == null)
        {
            ThrowError("No pending suggestion for this report.");
        }

        // Verify suggested cluster exists
        var cluster = await db.Clusters.FindAsync([report.SuggestedClusterId], ct);
        if (cluster == null)
        {
            ThrowError("The suggested cluster no longer exists.");
        }

        // Accept suggestion
        cluster.AddReport(report.Id);
        report.AssignToCluster(cluster.Id, report.SuggestedConfidenceScore);
        report.ChangeStatus(ReportStatus.Duplicate);
        report.ClearSuggestedCluster();

        if (cluster != null)
        {
            await clusterService.RecalculateCentroidAsync(cluster.Id, ct);
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Suggestion accepted. Report added to cluster." }, ct);
    }
}
