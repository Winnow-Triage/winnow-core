using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.SuggestActions;

/// <summary>
/// Request to accept a suggested cluster merge.
/// </summary>
public class AcceptSuggestionRequest
{
    /// <summary>
    /// ID of the report with the suggestion to accept.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class AcceptSuggestionEndpoint(WinnowDbContext db) : Endpoint<AcceptSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/accept-suggestion");
        Summary(s =>
        {
            s.Summary = "Accept clustering suggestion";
            s.Description = "Accepts the AI-suggested parent for a report, merging it into the cluster.";
            s.Response<ActionResponse>(200, "Suggestion accepted successfully");
            s.Response(400, "Invalid suggestion or circular reference");
            s.Response(404, "Report or suggested parent not found");
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

        if (report.SuggestedParentId == null)
        {
            AddError("Report has no suggested parent to accept.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        // Verify suggested parent exists in same project
        var suggestedParent = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == report.SuggestedParentId.Value && r.ProjectId == projectId, ct);
        if (suggestedParent == null)
        {
            AddError("Suggested parent not found in project.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var ultimateParentId = await db.ResolveUltimateMasterAsync(report.SuggestedParentId.Value, ct);

        if (ultimateParentId == report.Id)
        {
            AddError("Cannot accept suggestion: Circular reference detected.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        report.ParentReportId = ultimateParentId;
        report.Status = "Duplicate";
        report.ConfidenceScore = report.SuggestedConfidenceScore;

        report.SuggestedParentId = null;
        report.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Suggestion accepted successfully." }, ct);
    }
}
