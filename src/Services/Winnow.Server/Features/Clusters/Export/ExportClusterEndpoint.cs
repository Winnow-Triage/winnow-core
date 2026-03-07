using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.Export;

public class ExportClusterRequest
{
    public Guid ClusterId { get; set; }

    [FromHeader("X-Project-ID")]
    public Guid ProjectId { get; set; }

    public Guid ConfigId { get; set; }
}

public class ExportClusterResponse
{
    public Uri ExternalUrl { get; set; } = default!;
}

public sealed class ExportClusterEndpoint(
    WinnowDbContext db,
    IExporterFactory exporterFactory,
    IConfiguration config)
    : Endpoint<ExportClusterRequest, ExportClusterResponse>
{
    public override void Configure()
    {
        Post("/clusters/{ClusterId}/export");
        Options(x => x.RequireAuthorization());
        Summary(s =>
        {
            s.Summary = "Export a cluster";
            s.Description = "Exports a cluster summary to an external system (e.g., Jira, Linear).";
        });
    }

    public override async Task HandleAsync(ExportClusterRequest req, CancellationToken ct)
    {
        // 1. Validate ProjectId was actually provided
        if (req.ProjectId == Guid.Empty)
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return;
        }

        // 2. Fetch only the Cluster
        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == req.ClusterId && c.ProjectId == req.ProjectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // 3. Ask the DB for the exact stats we need (Incredibly fast, minimal memory)
        var reportStats = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .GroupBy(r => r.ClusterId)
            .Select(g => new
            {
                Count = g.Count(),
                FirstSeen = g.Min(r => r.CreatedAt),
                LastSeen = g.Max(r => r.CreatedAt)
            })
            .FirstOrDefaultAsync(ct);

        var reportCount = reportStats?.Count ?? 0;

        // 4. Ask the DB for ONLY the titles of the 10 most recent reports
        var topReports = await db.Reports
            .Where(r => r.ClusterId == cluster.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new { r.Id, r.Title })
            .ToListAsync(ct);

        var exporter = await exporterFactory.GetExporterByIdAsync(req.ConfigId, ct);

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

            var externalUrlString = await exporter.ExportReportAsync(cluster.Title ?? $"Cluster {cluster.Id.ToString()[..8]}", contentToExport, ct);
            var externalUrl = new Uri(externalUrlString);

            // Perfect use of the Domain method!
            cluster.ChangeStatus(ClusterStatus.Exported);
            await db.SaveChangesAsync(ct);

            await Send.OkAsync(new ExportClusterResponse { ExternalUrl = externalUrl }, ct);
        }
        catch (Exception ex)
        {
            AddError($"Export failed: {ex.Message}");
            ThrowIfAnyErrors();
        }
    }
}
