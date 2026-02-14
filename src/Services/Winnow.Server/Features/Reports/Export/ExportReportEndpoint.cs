using FastEndpoints;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Export;

public class ExportReportRequest
{
    public Guid ConfigId { get; set; }
}

public class ExportReportResponse
{
    public string ExternalUrl { get; set; } = string.Empty;
}

public class ExportReportEndpoint(WinnowDbContext db, ExporterFactory exporterFactory) : Endpoint<ExportReportRequest>
{
    public override void Configure()
    {
        Post("/reports/{Id}/export");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ExportReportRequest req, CancellationToken ct)
    {
        var reportId = Route<Guid>("Id");
        var report = await db.Reports.FindAsync([reportId], ct);

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

            var externalUrl = await exporter.ExportTicketAsync(report.Title, contentToExport ?? "", ct);
            
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
