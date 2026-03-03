using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;

namespace Winnow.Server.Features.Reports.SuggestActions;

/// <summary>
/// Request to dismiss a suggested cluster assignment.
/// </summary>
public class DismissSuggestionRequest
{
    /// <summary>
    /// ID of the report to dismiss the suggestion for.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class DismissSuggestionEndpoint(WinnowDbContext db, INegativeMatchCache negativeMatchCache) : Endpoint<DismissSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/dismiss-suggestion");
        Summary(s =>
        {
            s.Summary = "Dismiss a suggested cluster assignment";
            s.Description = "Dismisses the AI-suggested cluster for the specified report and records a negative match.";
            s.Response<ActionResponse>(200, "Suggestion dismissed");
            s.Response(400, "No pending suggestion");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(DismissSuggestionRequest req, CancellationToken ct)
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

        // Record negative match between report and cluster
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "default";
        negativeMatchCache.MarkAsMismatch(tenantId, report.Id, report.SuggestedClusterId.Value);

        // Clear suggestion
        report.SuggestedClusterId = null;
        report.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Suggestion dismissed." }, ct);
    }
}
