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

public class IngestReportRequest
{
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class IngestReportValidator : Validator<IngestReportRequest>
{
    public IngestReportValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000); 
    }
}

public record ReportCreatedEvent
{
    public Guid ReportId { get; init; }
    public Guid ProjectId { get; init; }
    public string Message { get; init; } = default!;
    public string? StackTrace { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Metadata { get; init; }
    public string? TenantId { get; init; }
}

public record IngestReportResponse
{
    public Guid Id { get; init; }
}

public class IngestReportEndpoint(
    IPublishEndpoint publishEndpoint,
    Winnow.Server.Services.Ai.IEmbeddingService embeddingService,
    WinnowDbContext dbContext,
    ILogger<IngestReportEndpoint> logger) : Endpoint<IngestReportRequest, IngestReportResponse>
{
    public override void Configure()
    {
        Post("/api/reports");
        AllowAnonymous();
        Description(b => b
            .Accepts<IngestReportRequest>("application/json")
            .Produces<IngestReportResponse>(202));
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
        var textToEmbed = $"{req.Message}\n{req.StackTrace}";
        var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);
        var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
        Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

        var report = new Report
        {
            ProjectId = project.Id,
            Message = req.Message,
            StackTrace = req.StackTrace,
            Metadata = req.Metadata != null ? JsonSerializer.Serialize(req.Metadata) : null,
            CreatedAt = DateTime.UtcNow,
            Embedding = embeddingBytes,
            ClusterId = null 
        };

        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("IngestReportEndpoint: Publishing ReportCreatedEvent for report {Id} (Tenant: {Tenant})", 
            report.Id, ((TenantContext)dbContext.GetService<ITenantContext>()).TenantId);

        await publishEndpoint.Publish(new ReportCreatedEvent
        {
            ReportId = report.Id,
            ProjectId = project.Id,
            Message = report.Message,
            StackTrace = report.StackTrace,
            CreatedAt = report.CreatedAt,
            Metadata = report.Metadata,
            TenantId = ((TenantContext)dbContext.GetService<ITenantContext>()).TenantId
        }, ct);

        await Send.AcceptedAtAsync("GetReport", new { id = report.Id }, new IngestReportResponse { Id = report.Id });
    }
}
