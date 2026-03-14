using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.Server.Domain.Assets.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Storage;

namespace Winnow.Server.Features.Reports.Get;

public record GetReportQuery(Guid ReportId, Guid ProjectId) : IRequest<GetReportResult>;

public record GetReportResult(bool IsSuccess, GetReportResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetReportHandler(WinnowDbContext db, IStorageService storageService, ILogger<GetReportHandler> logger) : IRequestHandler<GetReportQuery, GetReportResult>
{
    public async Task<GetReportResult> Handle(GetReportQuery request, CancellationToken cancellationToken)
    {
        var report = await db.Reports
            .AsNoTracking()
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(t => t.Id == request.ReportId && t.ProjectId == request.ProjectId, cancellationToken);

        if (report == null)
        {
            return new GetReportResult(false, null, "Report not found", 404);
        }

        var evidence = new List<RelatedReportDto>();

        if (report.ClusterId != null)
        {
            // Get other reports in the same cluster
            var clusterReports = await db.Reports
                .AsNoTracking()
                .Where(t => t.ProjectId == request.ProjectId && t.ClusterId == report.ClusterId && t.Id != report.Id)
                .Select(t => new RelatedReportDto
                {
                    Id = t.Id,
                    Message = t.Message,
                    Status = t.Status.Name,
                    CreatedAt = t.CreatedAt,
                    ConfidenceScore = t.ConfidenceScore != null ? (float)t.ConfidenceScore.Value.Score : null
                })
                .ToListAsync(cancellationToken);

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
                .FirstOrDefaultAsync(c => c.Id == report.ClusterId, cancellationToken);

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
                .FirstOrDefaultAsync(cancellationToken);

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
            .ToListAsync(cancellationToken);

        foreach (var asset in assetObjects)
        {
            Uri? downloadUrl = null;
            if (asset.Status == AssetStatus.Clean)
            {
                try
                {
                    downloadUrl = await storageService.GenerateDownloadUrlAsync(asset.S3Key, cancellationToken);
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

        var response = new GetReportResponse
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
        };

        return new GetReportResult(true, response);
    }
}
