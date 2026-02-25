using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Export;

/// <summary>
/// Request to export a report via an integration.
/// </summary>
public class ExportReportRequest
{
    /// <summary>
    /// ID of the integration configuration to use.
    /// </summary>
    public Guid ConfigId { get; set; }
}

/// <summary>
/// Response containing the external URL of the exported report.
/// </summary>
public class ExportReportResponse
{
    /// <summary>
    /// URL of the exported issue in the external system.
    /// </summary>
    public Uri ExternalUrl { get; set; } = default!;
}

public sealed class ExportReportEndpoint(WinnowDbContext db, IExporterFactory exporterFactory) : Endpoint<ExportReportRequest>
{
    public override void Configure()
    {
        Post("/reports/{Id}/export");
        Policies("RequireVerifiedEmail");
        Summary(s =>
        {
            s.Summary = "Export a report";
            s.Description = "Exports a report to an external system (e.g., Jira, GitHub) using a configured integration.";
            s.Response<ExportReportResponse>(200, "Report exported successfully");
            s.Response(404, "Report not found");
            s.Response(400, "Export failed");
        });
        Description(d => d.WithDescription("Email verification required to perform this action."));
    }

    public override async Task HandleAsync(ExportReportRequest req, CancellationToken ct)
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

        var reportId = Route<Guid>("Id");
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.ProjectId == projectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var exporter = await exporterFactory.GetExporterByIdAsync(req.ConfigId, ct);

        try
        {
            var contentToExport = string.IsNullOrWhiteSpace(report.Summary)
                ? report.StackTrace
                : $"## AI Perspective\n{report.Summary}\n\n## Original Details\n{report.StackTrace}";

            var backlink = $"http://localhost:5173/reports/{report.Id}";
            contentToExport += $"\n\n---\n[View in Winnow]({backlink})";

            var externalUrlString = await exporter.ExportReportAsync(report.Title, contentToExport ?? "", ct);
            var externalUrl = new Uri(externalUrlString);

            report.Status = "Exported";
            report.ExternalUrl = externalUrl;
            await db.SaveChangesAsync(ct);

            await Send.OkAsync(new ExportReportResponse { ExternalUrl = externalUrl }, ct);
        }
        catch (Exception ex)
        {
            AddError($"Export failed: {ex.Message}");
            ThrowIfAnyErrors();
        }
    }
}
