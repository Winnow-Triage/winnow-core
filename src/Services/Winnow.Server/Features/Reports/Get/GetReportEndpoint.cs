using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Assets.ValueObjects;
using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Reports;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Get;

/// <summary>
/// Request to retrieve a single report.
/// </summary>
public class GetReportRequest : ProjectScopedRequest
{
    /// <summary>
    /// The unique identifier of the report to retrieve.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Detailed response containing report data, assets, and evidence.
/// </summary>
public class GetReportResponse
{
    /// <summary>
    /// The unique identifier of the report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The project this report belongs to.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// The title or subject of the report.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The main message or description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The stack trace associated with the report, if applicable.
    /// </summary>
    public string? StackTrace { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the report (e.g., Open, Closed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the report was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Cluster fields
    /// <summary>
    /// ID of the cluster this report belongs to.
    /// </summary>
    public Guid? ClusterId { get; set; }

    /// <summary>
    /// Username or ID of the user assigned to this report.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// AI-generated cluster summary.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// AI-calculated confidence score (0-1).
    /// </summary>
    public float? ConfidenceScore { get; set; }

    /// <summary>
    /// Criticality score (1-100).
    /// </summary>
    public int? CriticalityScore { get; set; }

    /// <summary>
    /// Reasoning behind the criticality score.
    /// </summary>
    public string? CriticalityReasoning { get; set; }

    /// <summary>
    /// AI-generated cluster title.
    /// </summary>
    public string? ClusterTitle { get; set; }

    /// <summary>
    /// Suggested cluster ID from analysis.
    /// </summary>
    public Guid? SuggestedClusterId { get; set; }

    /// <summary>
    /// Confidence score for the suggested cluster.
    /// </summary>
    public float? SuggestedConfidenceScore { get; set; }

    /// <summary>
    /// Summary from the suggested cluster.
    /// </summary>
    public string? SuggestedClusterSummary { get; set; }

    /// <summary>
    /// Title from the suggested cluster.
    /// </summary>
    public string? SuggestedClusterTitle { get; set; }

    /// <summary>
    /// JSON metadata string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this report exceeded the free limits.
    /// </summary>
    public bool IsOverage { get; set; }

    /// <summary>
    /// Whether this report was held for ransom due to grace period breach.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// URL or path to a screenshot.
    /// </summary>
    public string? Screenshot { get; set; }

    /// <summary>
    /// External link related to the report.
    /// </summary>
    public Uri? ExternalUrl { get; set; }

    /// <summary>
    /// List of associated assets/files.
    /// </summary>
    public List<AssetDto> Assets { get; set; } = [];

    /// <summary>
    /// Related reports providing evidence or context.
    /// </summary>
    public List<RelatedReportDto> Evidence { get; set; } = [];
}

/// <summary>
/// Represents a file asset attached to a report.
/// </summary>
public class AssetDto
{
    /// <summary>
    /// Unique identifier for the asset.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Scan status: Pending, Clean, Infected, Failed.
    /// </summary>
    public string Status { get; set; } = string.Empty; // Pending, Clean, Infected, Failed

    /// <summary>
    /// Temporary download URL (if clean).
    /// </summary>
    public Uri? DownloadUrl { get; set; } // Presigned URL, only for Clean assets

    /// <summary>
    /// When the asset was uploaded.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the asset was scanned by antivirus.
    /// </summary>
    public DateTime? ScannedAt { get; set; }
}

/// <summary>
/// A related report used as evidence.
/// </summary>
public class RelatedReportDto
{
    /// <summary>
    /// Unique identifier of the related report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Brief message from the related report.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Status of the related report.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the related report was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Relevance score to the main report.
    /// </summary>
    public float? ConfidenceScore { get; set; }
}

public sealed class GetReportEndpoint(WinnowDbContext db, Services.Storage.IStorageService storageService, ILogger<GetReportEndpoint> logger) : ProjectScopedEndpoint<GetReportRequest, GetReportResponse>
{
    public override void Configure()
    {
        Get("/reports/{id:guid}");
        Description(x => x.WithName("GetReport"));
        Summary(s =>
        {
            s.Summary = "Retrieve a specific report";
            s.Description = "Fetches a report by its ID, including metadata, assets, and evidence.";
            s.Response<GetReportResponse>(200, "The requested report");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports
            .AsNoTracking()
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.ProjectId == req.CurrentProjectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var evidence = new List<RelatedReportDto>();

        if (report.ClusterId != null)
        {
            // Get other reports in the same cluster
            var clusterReports = await db.Reports
                .AsNoTracking()
                .Where(t => t.ProjectId == req.CurrentProjectId && t.ClusterId == report.ClusterId && t.Id != report.Id)
                .Select(t => new RelatedReportDto
                {
                    Id = t.Id,
                    Message = t.Message,
                    Status = t.Status.Name,
                    CreatedAt = t.CreatedAt,
                    ConfidenceScore = t.ConfidenceScore != null ? (float)t.ConfidenceScore.Value.Score : null
                })
                .ToListAsync(ct);

            evidence.AddRange(clusterReports);
        }

        // Load cluster metadata for summary/criticality
        string? clusterTitle = null;
        string? clusterSummary = null;
        int? criticalityScore = null;
        string? criticalityReasoning = null;

        if (report.ClusterId != null)
        {
            var cluster = await db.Clusters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == report.ClusterId, ct);

            if (cluster != null)
            {
                clusterTitle = cluster.Title;
                clusterSummary = cluster.Summary;
                criticalityScore = cluster.CriticalityScore;
                criticalityReasoning = cluster.CriticalityReasoning;
            }
        }

        // Load suggested cluster info
        string? suggestedClusterSummary = null;
        string? suggestedClusterTitle = null;
        if (report.SuggestedClusterId != null)
        {
            var suggestedCluster = await db.Clusters
                .AsNoTracking()
                .Where(c => c.Id == report.SuggestedClusterId)
                .Select(c => new { c.Summary, c.Title })
                .FirstOrDefaultAsync(ct);

            if (suggestedCluster != null)
            {
                suggestedClusterSummary = suggestedCluster.Summary;
                suggestedClusterTitle = suggestedCluster.Title;
            }
        }

        // Build asset DTOs with download URLs for clean files
        var assetDtos = new List<AssetDto>();
        var assetObjects = await db.Assets
            .Where(a => report.Assets.Contains(a.Id))
            .ToListAsync(ct);

        foreach (var asset in assetObjects)
        {
            Uri? downloadUrl = null;
            if (asset.Status == AssetStatus.Clean)
            {
                try
                {
                    downloadUrl = await storageService.GenerateDownloadUrlAsync(asset.S3Key, ct);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not generate download URL for asset {AssetId}", asset.Id);
                }
            }

            assetDtos.Add(new AssetDto
            {
                Id = asset.Id,
                FileName = asset.FileName,
                ContentType = asset.ContentType,
                SizeBytes = asset.SizeBytes,
                Status = asset.Status.ToString(),
                DownloadUrl = downloadUrl,
                CreatedAt = asset.CreatedAt,
                ScannedAt = asset.ScannedAt
            });
        }

        await Send.OkAsync(new GetReportResponse
        {
            Id = report.Id,
            ProjectId = report.ProjectId,
            Title = report.Title,
            Message = report.Message,
            StackTrace = report.StackTrace,
            Status = report.Status.Name,
            CreatedAt = report.CreatedAt,
            ClusterId = report.ClusterId,
            AssignedTo = report.AssignedTo,
            Summary = clusterSummary,
            ConfidenceScore = (float?)report.ConfidenceScore?.Score,
            CriticalityScore = criticalityScore,
            CriticalityReasoning = criticalityReasoning,
            ClusterTitle = clusterTitle,
            SuggestedClusterId = report.SuggestedClusterId,
            SuggestedConfidenceScore = (float?)report.SuggestedConfidenceScore?.Score,
            SuggestedClusterSummary = suggestedClusterSummary,
            SuggestedClusterTitle = suggestedClusterTitle,
            Metadata = report.Metadata,
            IsOverage = report.IsOverage,
            IsLocked = report.IsLocked,
            ExternalUrl = report.ExternalUrl,
            Assets = assetDtos,
            Evidence = evidence
        }, ct);
    }
}
