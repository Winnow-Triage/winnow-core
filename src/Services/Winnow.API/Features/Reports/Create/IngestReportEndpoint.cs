using System.Text.Json;
using Winnow.Contracts;
using FastEndpoints;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using Winnow.API.Extensions;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security.PoW;
using Microsoft.AspNetCore.Mvc;

namespace Winnow.API.Features.Reports.Create;

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
    /// Size of the screenshot in bytes. Required if ScreenshotKey is provided.
    /// </summary>
    public long? ScreenshotSize { get; set; }

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
        RuleFor(x => x.StackTrace).MaximumLength(10000).When(x => !string.IsNullOrEmpty(x.StackTrace));

        RuleFor(x => x.Metadata)
            .Must(m => m!.Count <= 10)
            .WithMessage("Metadata cannot contain more than 10 entries.")
            .Must(m => m!.Keys.All(k => k.Length <= 64))
            .WithMessage("Metadata keys cannot exceed 64 characters.")
            .When(x => x.Metadata != null);

        RuleFor(x => x.ScreenshotKey)
            .MustBeValidFilePath()
            .When(x => !string.IsNullOrEmpty(x.ScreenshotKey));
    }
}



/// <summary>
/// Response after successful ingestion.
/// </summary>
public record IngestReportResponse
{
    public Guid Id { get; init; }
}

[RequestSizeLimit(65536)] // 64KB - WIN-102 Hardening
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("webhook")]
public sealed class IngestReportEndpoint(
    IMediator mediator,
    WinnowDbContext dbContext,
    ILogger<IngestReportEndpoint> logger) : Endpoint<IngestReportRequest, IngestReportResponse>
{
    public override void Configure()
    {
        Post("/reports");
        AuthSchemes("ApiKey");
        PreProcessor<PoWPreProcessor<IngestReportRequest>>();

        // Ensure standard .NET rate limiting policies are applied
        Options(x => x.RequireRateLimiting("webhook"));

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

        if (currentOrgId == Guid.Empty)
        {
            logger.LogWarning("Report ingestion failed. Organization not found in context or claims.");
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var command = new CreateReportCommand(
            currentOrgId,
            projectId,
            req.Title,
            req.Message,
            req.StackTrace,
            req.ScreenshotKey,
            req.ScreenshotSize,
            req.Metadata
        );

        var reportId = await mediator.Send(command, ct);

        await Send.AcceptedAtAsync("GetReport", new { id = reportId }, new IngestReportResponse { Id = reportId }, false, ct);
    }
}