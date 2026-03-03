using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Services;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.SuggestActions;

/// <summary>
/// Request to accept a suggested cluster assignment.
/// </summary>
public class AcceptSuggestionRequest
{
    /// <summary>
    /// ID of the report to accept a suggestion for.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class AcceptSuggestionEndpoint(WinnowDbContext db, IClusterService clusterService) : Endpoint<AcceptSuggestionRequest, ActionResponse>
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
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        // Validate user owns this project
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == projectId, ct);

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
        report.ClusterId = report.SuggestedClusterId;
        report.Status = "Duplicate";
        report.ConfidenceScore = report.SuggestedConfidenceScore;
        report.SuggestedClusterId = null;
        report.SuggestedConfidenceScore = null;

        if (cluster != null)
        {
            await clusterService.RecalculateCentroidAsync(cluster.Id, ct);
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Suggestion accepted. Report added to cluster." }, ct);
    }
}
