using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Reports.Export;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Reports.Export;

[RequirePermission("reports:write")]
public record ExportReportCommand(Guid OrgId, Guid ReportId, Guid ProjectId, string UserId, Guid ConfigId) : IRequest<ExportReportResult>, IOrgScopedRequest;

public record ExportReportResult(bool IsSuccess, ExportReportResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ExportReportHandler(WinnowDbContext db, IExporterFactory exporterFactory) : IRequestHandler<ExportReportCommand, ExportReportResult>
{
    public async Task<ExportReportResult> Handle(ExportReportCommand request, CancellationToken cancellationToken)
    {
        // Validate user owns this project
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProjectId && p.OwnerId == request.UserId, cancellationToken);

        if (!userOwnsProject)
        {
            return new ExportReportResult(false, null, "Project not found or access denied", 404);
        }

        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == request.ReportId && r.ProjectId == request.ProjectId, cancellationToken);

        if (report == null)
        {
            return new ExportReportResult(false, null, "Report not found", 404);
        }

        var exporter = await exporterFactory.GetExporterByIdAsync(request.ConfigId, cancellationToken);

        try
        {
            // Get cluster summary if available
            string? clusterSummary = null;
            if (report.ClusterId != null)
            {
                clusterSummary = await db.Clusters
                    .AsNoTracking()
                    .Where(c => c.Id == report.ClusterId)
                    .Select(c => c.Summary)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var contentToExport = string.IsNullOrWhiteSpace(clusterSummary)
                ? report.StackTrace
                : $"## AI Perspective\n{clusterSummary}\n\n## Original Details\n{report.StackTrace}";

            var backlink = $"http://localhost:5173/reports/{report.Id}";
            contentToExport += $"\n\n---\n[View in Winnow]({backlink})";

            var externalUrlString = await exporter.ExportReportAsync(report.Title, contentToExport ?? "", cancellationToken);
            var externalUrl = new Uri(externalUrlString);

            report.MarkAsExported(externalUrl);
            await db.SaveChangesAsync(cancellationToken);

            return new ExportReportResult(true, new ExportReportResponse { ExternalUrl = externalUrl });
        }
        catch (Exception ex)
        {
            return new ExportReportResult(false, null, $"Export failed: {ex.Message}", 400);
        }
    }
}
