using System.Text.Json;
using FastEndpoints;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Create;

/// <summary>
/// Data required to ingest a new report.
/// </summary>
public class IngestReportRequest
{
    /// <summary>
    /// Title of the report.
    /// </summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// Detailed message or description.
    /// </summary>
    public string Message { get; set; } = default!;

    /// <summary>
    /// Optional stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Base64 encoded screenshot image. (Deprecated in favor of direct S3 upload ScreenshotKey)
    /// </summary>
    public string? Screenshot { get; set; }

    /// <summary>
    /// S3 object key returned from the presigned URL flow for direct uploads.
    /// </summary>
    public string? ScreenshotKey { get; set; }

    /// <summary>
    /// Arbitrary metadata key-value pairs.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

internal class IngestReportValidator : Validator<IngestReportRequest>
{
    public IngestReportValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
    }
}

internal record ReportCreatedEvent
{
    public Guid ReportId { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? StackTrace { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Metadata { get; init; }
    public string? TenantId { get; init; }
}

/// <summary>
/// Response after successful ingestion.
/// </summary>
public record IngestReportResponse
{
    /// <summary>
    /// The ID of the created report.
    /// </summary>
    public Guid Id { get; init; }
}

public sealed class IngestReportEndpoint(
    IPublishEndpoint publishEndpoint,
    Winnow.Server.Services.Ai.IEmbeddingService embeddingService,
    Winnow.Server.Services.Storage.IStorageService storageService,
    Winnow.Server.Services.Quota.IQuotaService quotaService,
    WinnowDbContext dbContext,
    ILogger<IngestReportEndpoint> logger) : Endpoint<IngestReportRequest, IngestReportResponse>
{
    public override void Configure()
    {
        Post("/reports");
        AuthSchemes("ApiKey");
        Description(b => b
            .WithName("IngestReport")
            .Accepts<IngestReportRequest>("application/json")
            .Produces<IngestReportResponse>(202));
        Summary(s =>
        {
            s.Summary = "Ingest a new report";
            s.Description = "Accepts a new report via API key authentication. Processes embeddings and screenshots.";
            s.Response<IngestReportResponse>(202, "Report accepted for processing");
            s.Response(401, "Invalid API Key");
        });
    }

    public override async Task HandleAsync(IngestReportRequest req, CancellationToken ct)
    {
        // Authentication handled by ApiKey scheme
        var projectIdClaim = User.FindFirst("ProjectId");
        if (projectIdClaim == null || !Guid.TryParse(projectIdClaim.Value, out var projectId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var tenantContext = dbContext.GetService<ITenantContext>();
        var currentTenantId = ((TenantContext)tenantContext).TenantId;
        var currentOrgId = tenantContext.CurrentOrganizationId ?? Guid.Empty;

        if (currentOrgId == Guid.Empty)
        {
            var orgClaim = User.FindFirst("organization");
            if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var parsedOrgId))
            {
                currentOrgId = parsedOrgId;
            }
        }

        // 1. Generate Embedding
        var textToEmbed = $"{req.Title}\n{req.Message}\n{req.StackTrace}";
        var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);
        var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
        Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

        // 2. Generate Report ID upfront so we can use it in the S3 path
        var reportId = Guid.NewGuid();

        // 3. Asset record creation map
        Asset? screenshotAsset = null;
        if (!string.IsNullOrEmpty(req.ScreenshotKey))
        {
            var fileName = Path.GetFileName(req.ScreenshotKey);
            screenshotAsset = new Asset
            {
                OrganizationId = currentOrgId,
                ProjectId = projectId,
                ReportId = reportId,
                FileName = string.IsNullOrEmpty(fileName) ? "screenshot.png" : fileName,
                S3Key = req.ScreenshotKey,
                ContentType = "image/png", // We default to PNG as that's what SDK produces
                SizeBytes = 0, // For direct uploads, we don't know the exact size upfront here
                Status = AssetStatus.Pending
            };
            logger.LogInformation("Asset created for directly uploaded S3 Key: {Key}", req.ScreenshotKey);
        }
        else if (!string.IsNullOrEmpty(req.Screenshot))
        {
            // Legacy Base64 Flow
            try
            {
                // Strip data URL prefix: "data:image/png;base64,..."
                var base64Data = req.Screenshot;
                var commaIndex = base64Data.IndexOf(',');
                if (commaIndex >= 0)
                    base64Data = base64Data[(commaIndex + 1)..];

                var imageBytes = Convert.FromBase64String(base64Data);
                var fileName = $"screenshot_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                using var stream = new MemoryStream(imageBytes);

                // Direct SDK upload with report-scoped path
                var s3Key = await storageService.UploadFileAsync(
                    Guid.Empty, // orgId — will be populated when multi-tenancy is wired
                    projectId,
                    reportId,
                    stream,
                    fileName,
                    "image/png",
                    currentTenantId, ct);

                screenshotAsset = new Asset
                {
                    OrganizationId = Guid.Empty,
                    ProjectId = projectId,
                    ReportId = reportId,
                    FileName = fileName,
                    S3Key = s3Key,
                    ContentType = "image/png",
                    SizeBytes = imageBytes.Length,
                    Status = AssetStatus.Pending
                };

                logger.LogInformation("Screenshot uploaded to quarantine: {Key}", s3Key);
            }
            catch (Exception ex)
            {
                // Don't fail report ingestion if S3 is unavailable
                logger.LogWarning(ex, "Failed to upload screenshot to S3 — skipping");
            }
        }

        // Quota Evaluation - Evaluate Grace Period and Ransom triggers (do NOT block the request)
        var quotaStatus = await quotaService.GetIngestionQuotaStatusAsync(currentOrgId, ct);
        if (quotaStatus.isLocked)
        {
            // The Grace Limit was breached on this request, perform retroactive lock
            await quotaService.EnforceRetroactiveRansomAsync(currentOrgId, ct);
        }

        var report = new Report
        {
            Id = reportId,
            ProjectId = projectId,
            OrganizationId = currentOrgId,
            Title = req.Title,
            Message = req.Message,
            StackTrace = req.StackTrace,
            Metadata = req.Metadata != null ? JsonSerializer.Serialize(req.Metadata) : null,
            CreatedAt = DateTime.UtcNow,
            Embedding = embeddingBytes,
            ClusterId = null,
            IsOverage = quotaStatus.isOverage,
            IsLocked = quotaStatus.isLocked
        };

        dbContext.Reports.Add(report);
        if (screenshotAsset != null)
            dbContext.Assets.Add(screenshotAsset);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("IngestReportEndpoint: Publishing ReportCreatedEvent for report {Id} (Tenant: {Tenant})",
            report.Id, ((TenantContext)dbContext.GetService<ITenantContext>()).TenantId);

        await publishEndpoint.Publish(new ReportCreatedEvent
        {
            ReportId = report.Id,
            ProjectId = projectId,
            Title = report.Title,
            Message = report.Message,
            StackTrace = report.StackTrace,
            CreatedAt = report.CreatedAt,
            Metadata = report.Metadata,
            TenantId = ((TenantContext)dbContext.GetService<ITenantContext>()).TenantId
        }, ct);

        await Send.AcceptedAtAsync("GetReport", new { id = report.Id }, new IngestReportResponse { Id = report.Id }, false, ct);
    }
}
