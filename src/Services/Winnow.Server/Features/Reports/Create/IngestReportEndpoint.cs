using System.Text.Json;
using FastEndpoints;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Winnow.Server.Extensions;
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
        RuleFor(x => x.ScreenshotKey)
            .MustBeValidFilePath()
            .When(x => !string.IsNullOrEmpty(x.ScreenshotKey));
    }
}

internal record ReportCreatedEvent
{
    public Guid ReportId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string? StackTrace { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Metadata { get; init; }
}

/// <summary>
/// Response after successful ingestion.
/// </summary>
public record IngestReportResponse
{
    public Guid Id { get; init; }
}

public sealed class IngestReportEndpoint(
    IPublishEndpoint publishEndpoint,
    Services.Ai.IEmbeddingService embeddingService,
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
        var projectIdClaim = User.FindFirst("ProjectId");
        if (projectIdClaim == null || !Guid.TryParse(projectIdClaim.Value, out var projectId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var tenantContext = dbContext.GetService<ITenantContext>();
        var currentOrgId = tenantContext.CurrentOrganizationId ?? Guid.Empty;

        if (currentOrgId == Guid.Empty)
        {
            var orgClaim = User.FindFirst("organization");
            if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var parsedOrgId))
            {
                currentOrgId = parsedOrgId;
            }
        }

        // Load the Aggregate that owns the rules
        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentOrgId, ct);

        if (organization == null)
        {
            logger.LogWarning("Report ingestion failed. Organization {OrgId} not found.", currentOrgId);
            await Send.NotFoundAsync(ct);
            return;
        }

        // Generate Embedding
        var textToEmbed = $"{req.Title}\n{req.Message}\n{req.StackTrace}";
        var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);

        // Generate Report ID upfront
        var reportId = Guid.NewGuid();

        // Asset record creation map
        Domain.Assets.Asset? screenshotAsset = null;
        if (!string.IsNullOrEmpty(req.ScreenshotKey))
        {
            var fileName = Path.GetFileName(req.ScreenshotKey);
            screenshotAsset = new Domain.Assets.Asset(
                currentOrgId,
                projectId,
                reportId,
                string.IsNullOrEmpty(fileName) ? "screenshot.png" : fileName,
                req.ScreenshotKey,
                0,
                "image/png"
            );
        }

        var report = new Domain.Reports.Report(
            projectId,
            currentOrgId,
            req.Title,
            req.Message,
            req.StackTrace,
            null,
            embeddingFloats,
            null,
            isOverage: organization.ReportQuota.IsOverage(),
            isLocked: organization.IsLocked
        );

        dbContext.Reports.Add(report);

        if (screenshotAsset != null)
            dbContext.Assets.Add(screenshotAsset);

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("IngestReportEndpoint: Publishing ReportCreatedEvent for report {Id} (Org: {OrgId})", report.Id, currentOrgId);

        await publishEndpoint.Publish(new ReportCreatedEvent
        {
            ReportId = report.Id,
            OrganizationId = currentOrgId,
            ProjectId = projectId,
            Title = report.Title,
            Message = report.Message,
            StackTrace = report.StackTrace,
            CreatedAt = report.CreatedAt,
            Metadata = report.Metadata != null ? JsonSerializer.Serialize(report.Metadata) : null
        }, ct);

        await Send.AcceptedAtAsync("GetReport", new { id = report.Id }, new IngestReportResponse { Id = report.Id }, false, ct);
    }
}