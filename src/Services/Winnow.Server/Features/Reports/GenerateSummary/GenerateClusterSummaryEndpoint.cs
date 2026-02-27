using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.GenerateSummary;

/// <summary>
/// Request to generate an AI summary for a cluster.
/// </summary>
public class GenerateClusterSummaryRequest
{
    /// <summary>
    /// ID of the cluster/report to summarize.
    /// </summary>
    public Guid Id { get; set; }
}

public sealed class GenerateClusterSummaryEndpoint(WinnowDbContext db, IClusterSummaryService summaryService) : Endpoint<GenerateClusterSummaryRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{Id}/generate-summary");
        Summary(s =>
        {
            s.Summary = "Generate cluster summary";
            s.Description = "Triggers AI generation of a summary and criticality score for a report cluster.";
            s.Response<ActionResponse>(200, "Summary generated successfully");
            s.Response(404, "Report not found");
            s.Response(500, "AI generation failed");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GenerateClusterSummaryRequest req, CancellationToken ct)
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

        // Fetch related reports (evidence) to provide context for the summary - filter by project
        // Including the root report itself so the AI has access to it even if no children exist
        var relatedReports = await db.Reports
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && (t.Id == report.Id || t.ParentReportId == report.Id))
            .ToListAsync(ct);

        var result = await summaryService.GenerateSummaryAsync(relatedReports, ct);

        if (result.IsError)
        {
            Logger.LogWarning("AI summary generation failed for report {ReportId}: {Error}", req.Id, result.Summary);
            HttpContext.Response.StatusCode = 500;
            await Send.OkAsync(new ActionResponse { Message = "AI summary generation failed. Please try again." }, ct);
            return;
        }

        report.Summary = result.Summary;
        report.CriticalityScore = result.CriticalityScore;
        report.CriticalityReasoning = result.CriticalityReasoning;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Summary generated successfully." }, ct);
    }
}
