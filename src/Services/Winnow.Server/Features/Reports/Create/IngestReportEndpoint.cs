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
    /// Base64 encoded screenshot image.
    /// </summary>
    public string? Screenshot { get; set; }

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
    WinnowDbContext dbContext,
    ILogger<IngestReportEndpoint> logger) : Endpoint<IngestReportRequest, IngestReportResponse>
{
    public override void Configure()
    {
        Post("/reports");
        AllowAnonymous();
        Description(b => b
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
        if (!HttpContext.Request.Headers.TryGetValue("X-Winnow-Key", out var apiKeyValues))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApiKey == apiKey, ct);

        if (project == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // 1. Generate Embedding
        var textToEmbed = $"{req.Title}\n{req.Message}";
        var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);
        var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
        Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

        // 2. Generate Report ID upfront so we can use it in the S3 path
        var reportId = Guid.NewGuid();

        // 3. Upload screenshot to S3 quarantine and create Asset record (if provided)
        Asset? screenshotAsset = null;
        if (!string.IsNullOrEmpty(req.Screenshot))
        {
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
                var currentTenantId = ((TenantContext)dbContext.GetService<ITenantContext>()).TenantId;
                var s3Key = await storageService.UploadFileAsync(
                    Guid.Empty, // orgId — will be populated when multi-tenancy is wired
                    project.Id,
                    reportId,
                    stream,
                    fileName,
                    "image/png",
                    currentTenantId, ct);

                screenshotAsset = new Asset
                {
                    OrganizationId = Guid.Empty,
                    ProjectId = project.Id,
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

        var report = new Report
        {
            Id = reportId,
            ProjectId = project.Id,
            Title = req.Title,
            Message = req.Message,
            StackTrace = req.StackTrace,
            Metadata = req.Metadata != null ? JsonSerializer.Serialize(req.Metadata) : null,
            CreatedAt = DateTime.UtcNow,
            Embedding = embeddingBytes,
            ClusterId = null
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
            ProjectId = project.Id,
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
