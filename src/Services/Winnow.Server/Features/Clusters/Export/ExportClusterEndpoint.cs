using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Integrations;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Clusters.Export;

public class ExportClusterRequest
{
    public Guid ConfigId { get; set; }
}

public class ExportClusterResponse
{
    public Uri ExternalUrl { get; set; } = default!;
}

public sealed class ExportClusterEndpoint(WinnowDbContext db, IExporterFactory exporterFactory) : Endpoint<ExportClusterRequest>
{
    public override void Configure()
    {
        Post("/clusters/{Id}/export");
        Options(x => x.RequireAuthorization());
        Summary(s =>
        {
            s.Summary = "Export a cluster";
            s.Description = "Exports a cluster summary to an external system (e.g., Jira, Linear).";
        });
    }

    public override async Task HandleAsync(ExportClusterRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader) ||
            !Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Valid Project ID is required in X-Project-ID header", 400);
            return;
        }

        var clusterId = Route<Guid>("Id");
        var cluster = await db.Clusters
            .Include(c => c.Reports)
            .FirstOrDefaultAsync(c => c.Id == clusterId && c.ProjectId == projectId, ct);

        if (cluster == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
                Report Count: {cluster.Reports.Count}
                First Seen: {cluster.Reports.Min(r => r.CreatedAt)}
                Last Seen: {cluster.Reports.Max(r => r.CreatedAt)}

                ## Reports
                {string.Join("\n", cluster.Reports.OrderByDescending(r => r.CreatedAt).Take(10).Select(r => $"- {r.Title} (R-{r.Id.ToString()[..8]})"))}
                {(cluster.Reports.Count > 10 ? $"\n... and {cluster.Reports.Count - 10} more" : "")}
                """;

            var backlink = $"http://localhost:5173/clusters/{cluster.Id}";
            contentToExport += $"\n\n---\n[View in Winnow]({backlink})";

            var externalUrlString = await exporter.ExportReportAsync(cluster.Title ?? $"Cluster {cluster.Id.ToString()[..8]}", contentToExport, ct);
            var externalUrl = new Uri(externalUrlString);

            cluster.Status = "Exported";
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
