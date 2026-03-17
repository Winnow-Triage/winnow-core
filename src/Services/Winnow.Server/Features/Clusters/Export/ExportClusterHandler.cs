using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Clusters.Export;

[RequirePermission("clusters:write")]
public record ExportClusterCommand(Guid OrgId, Guid ClusterId, Guid ProjectId, Guid ConfigId) : IRequest<ExportClusterResult>, IOrgScopedRequest;

public record ExportClusterResult(bool IsSuccess, Uri? ExternalUrl, string? ErrorMessage = null, int? StatusCode = null);

public class ExportClusterHandler(
    WinnowDbContext db,
    IExporterFactory exporterFactory,
    IConfiguration config,
    ILogger<ExportClusterHandler> logger) : IRequestHandler<ExportClusterCommand, ExportClusterResult>
{
    public async Task<ExportClusterResult> Handle(ExportClusterCommand request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == request.ClusterId && c.ProjectId == request.ProjectId, cancellationToken);

        if (cluster == null)
        {
            return new ExportClusterResult(false, null, "Cluster not found", 404);
        }

        var reportStats = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .GroupBy(r => r.ClusterId)
            .Select(g => new
            {
                Count = g.Count(),
                FirstSeen = g.Min(r => r.CreatedAt),
                LastSeen = g.Max(r => r.CreatedAt)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var reportCount = reportStats?.Count ?? 0;

        var topReports = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new { r.Id, r.Title })
            .ToListAsync(cancellationToken);

        var exporter = await exporterFactory.GetExporterByIdAsync(request.ConfigId, cancellationToken);

        try
        {
            var contentToExport = $"""
                ## AI Synthesis
                {cluster.Summary ?? "No summary available."}

                ## Criticality Analysis
                Score: {cluster.CriticalityScore}/10
                Reasoning: {cluster.CriticalityReasoning}

                ## Impact
                Report Count: {reportCount}
                First Seen: {reportStats?.FirstSeen.ToString("g") ?? "N/A"}
                Last Seen: {reportStats?.LastSeen.ToString("g") ?? "N/A"}

                ## Reports
                {string.Join("\n", topReports.Select(r => $"- {r.Title} (R-{r.Id.ToString()[..8]})"))}
                {(reportCount > 10 ? $"\n... and {reportCount - 10} more" : "")}
                """;

            var frontendUrl = config["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
            var backlink = $"{frontendUrl}/clusters/{cluster.Id}";
            contentToExport += $"\n\n---\n[View in Winnow]({backlink})";

            var externalUrlString = await exporter.ExportReportAsync(cluster.Title ?? $"Cluster {cluster.Id.ToString()[..8]}", contentToExport, cancellationToken);
            var externalUrl = new Uri(externalUrlString);

            cluster.ChangeStatus(ClusterStatus.Exported);
            await db.SaveChangesAsync(cancellationToken);

            return new ExportClusterResult(true, externalUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export failed for cluster {ClusterId}", cluster.Id);
            return new ExportClusterResult(false, null, $"Export failed: {ex.Message}", 500);
        }
    }
}
