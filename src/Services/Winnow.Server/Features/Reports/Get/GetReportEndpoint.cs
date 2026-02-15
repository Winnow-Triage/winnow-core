using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Get;

public class GetReportRequest
{
    public Guid Id { get; set; }
}

public class GetReportResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Clustering/Legacy fields
    public Guid? ParentReportId { get; set; }
    public string? AssignedTo { get; set; }
    public string? Summary { get; set; }
    public float? ConfidenceScore { get; set; }
    public int? CriticalityScore { get; set; }
    public string? CriticalityReasoning { get; set; }
    public string? ParentReportMessage { get; set; } // Was ParentTicketTitle
    public Guid? SuggestedParentId { get; set; }
    public float? SuggestedConfidenceScore { get; set; }
    public string? SuggestedParentMessage { get; set; } // Was SuggestedParentTitle
    public string? Metadata { get; set; }
    public string? Screenshot { get; set; }
    public string? ExternalUrl { get; set; }

    public List<AssetDto> Assets { get; set; } = [];
    public List<RelatedReportDto> Evidence { get; set; } = [];
}

public class AssetDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Clean, Infected, Failed
    public string? DownloadUrl { get; set; } // Presigned URL, only for Clean assets
    public DateTime CreatedAt { get; set; }
    public DateTime? ScannedAt { get; set; }
}

public class RelatedReportDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public float? ConfidenceScore { get; set; }
}

public class GetReportEndpoint(WinnowDbContext db, Winnow.Server.Services.Storage.IStorageService storageService, ILogger<GetReportEndpoint> logger) : Endpoint<GetReportRequest, GetReportResponse>
{
    public override void Configure()
    {
        Get("/reports/{id}");
        AllowAnonymous();
        Description(x => x.WithName("GetReport"));
    }

    public override async Task HandleAsync(GetReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports
            .AsNoTracking()
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var evidence = new List<RelatedReportDto>();

        if (report.ParentReportId == null)
        {
            // cluster parent
            var children = await db.Reports
                .AsNoTracking()
                .Where(t => t.ParentReportId == report.Id)
                .Select(t => new RelatedReportDto
                {
                    Id = t.Id,
                    Message = t.Message,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    ConfidenceScore = t.ConfidenceScore
                })
                .ToListAsync(ct);

            evidence.AddRange(children);
        }

        string? parentReportMessage = null;
        if (report.ParentReportId != null)
        {
            parentReportMessage = await db.Reports
                .AsNoTracking()
                .Where(t => t.Id == report.ParentReportId)
                .Select(t => t.Message)
                .FirstOrDefaultAsync(ct);
        }

        string? suggestedParentMessage = null;
        if (report.SuggestedParentId != null)
        {
            suggestedParentMessage = await db.Reports
                .AsNoTracking()
                .Where(t => t.Id == report.SuggestedParentId)
                .Select(t => t.Message)
                .FirstOrDefaultAsync(ct);
        }

        // Build asset DTOs with download URLs for clean files
        var assetDtos = new List<AssetDto>();
        foreach (var asset in report.Assets)
        {
            string? downloadUrl = null;
            if (asset.Status == AssetStatus.Clean)
            {
                try
                {
                    // Use the S3Key (which Bouncer may have updated to the clean bucket key)
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
            Title = report.Title,
            Message = report.Message,
            StackTrace = report.StackTrace,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
            ParentReportId = report.ParentReportId,
            AssignedTo = report.AssignedTo,
            Summary = report.Summary,
            ConfidenceScore = report.ConfidenceScore,
            CriticalityScore = report.CriticalityScore,
            CriticalityReasoning = report.CriticalityReasoning,
            ParentReportMessage = parentReportMessage,
            SuggestedParentId = report.SuggestedParentId,
            SuggestedConfidenceScore = report.SuggestedConfidenceScore,
            SuggestedParentMessage = suggestedParentMessage,
            Metadata = report.Metadata,
            ExternalUrl = report.ExternalUrl,
            Assets = assetDtos,
            Evidence = evidence
        }, ct);
    }
}
