using System.Security.Claims;
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
    public string? ParentReportMessage { get; set; }
    public Guid? SuggestedParentId { get; set; }
    public float? SuggestedConfidenceScore { get; set; }
    public string? SuggestedParentMessage { get; set; }
    public string? Metadata { get; set; }
    public string? Screenshot { get; set; }
    public Uri? ExternalUrl { get; set; }

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
    public Uri? DownloadUrl { get; set; } // Presigned URL, only for Clean assets
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

public sealed class GetReportEndpoint(WinnowDbContext db, Winnow.Server.Services.Storage.IStorageService storageService, ILogger<GetReportEndpoint> logger) : Endpoint<GetReportRequest, GetReportResponse>
{
    public override void Configure()
    {
        Get("/reports/{id}");
        Description(x => x.WithName("GetReport"));
    }

    public override async Task HandleAsync(GetReportRequest req, CancellationToken ct)
    {
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

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
            .AsNoTracking()
            .Include(r => r.Assets)
            .FirstOrDefaultAsync(t => t.Id == req.Id && t.ProjectId == projectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var evidence = new List<RelatedReportDto>();

        if (report.ParentReportId == null)
        {
            // cluster parent - filter by project ID
            var children = await db.Reports
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId && t.ParentReportId == report.Id)
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
                .Where(t => t.ProjectId == projectId && t.Id == report.ParentReportId)
                .Select(t => t.Message)
                .FirstOrDefaultAsync(ct);
        }

        string? suggestedParentMessage = null;
        if (report.SuggestedParentId != null)
        {
            suggestedParentMessage = await db.Reports
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId && t.Id == report.SuggestedParentId)
                .Select(t => t.Message)
                .FirstOrDefaultAsync(ct);
        }

        // Build asset DTOs with download URLs for clean files
        var assetDtos = new List<AssetDto>();
        foreach (var asset in report.Assets)
        {
            Uri? downloadUrl = null;
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
