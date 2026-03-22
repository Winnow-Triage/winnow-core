using System.Text.Json;
using Winnow.Contracts;
using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Reports;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.API.Services.Quota;
using Winnow.API.Domain.Ai;

namespace Winnow.API.Features.Reports.Create;

public record CreateReportCommand(
    Guid OrganizationId,
    Guid ProjectId,
    string Title,
    string Message,
    string? StackTrace = null,
    string? ScreenshotKey = null,
    Dictionary<string, object>? Metadata = null) : IRequest<Guid>;

public class CreateReportHandler(
    WinnowDbContext dbContext,
    IQuotaService quotaService,
    IEmbeddingService embeddingService,
    IPublishEndpoint publishEndpoint,
    ILogger<CreateReportHandler> logger) : IRequestHandler<CreateReportCommand, Guid>
{
    public async Task<Guid> Handle(CreateReportCommand request, CancellationToken ct)
    {
        // 1. Quota check
        var quotaStatus = await quotaService.GetIngestionQuotaStatusAsync(request.OrganizationId, ct);
        if (quotaStatus.isLocked)
        {
            await quotaService.EnforceRetroactiveRansomAsync(request.OrganizationId, ct);
        }

        // 2. Embedding generation
        var textToEmbed = $"{request.Title}\n{request.Message}";
        if (!string.IsNullOrWhiteSpace(request.StackTrace))
        {
            textToEmbed += $"\n{request.StackTrace}";
        }
        var embeddingResult = await embeddingService.GetEmbeddingAsync(textToEmbed);
        var embeddingFloats = embeddingResult.Vector;

        // Log Usage for auditing
        if (embeddingResult.Usage != null)
        {
            dbContext.AiUsages.Add(new Domain.Ai.AiUsage(
                request.OrganizationId,
                "EmbeddingGeneration",
                embeddingResult.Usage.Provider,
                embeddingResult.Usage.ModelId,
                embeddingResult.Usage.PromptTokens,
                embeddingResult.Usage.CompletionTokens
            ));
        }

        // 3. Create Report entity
        var report = new Report(
            request.ProjectId,
            request.OrganizationId,
            request.Title,
            request.Message,
            request.StackTrace,
            null,
            embeddingFloats,
            null,
            isOverage: quotaStatus.isOverage,
            isLocked: quotaStatus.isLocked
        );

        if (request.Metadata != null)
        {
            report.UpdateMetadata(JsonSerializer.Serialize(request.Metadata));
        }

        if (!string.IsNullOrEmpty(request.ScreenshotKey))
        {
            report.SetScreenshot(request.ScreenshotKey);
        }

        dbContext.Reports.Add(report);

        // 4. Handle Screenshot Asset if provided
        if (!string.IsNullOrEmpty(request.ScreenshotKey))
        {
            var fileName = System.IO.Path.GetFileName(request.ScreenshotKey);
            var screenshotAsset = new Domain.Assets.Asset(
                request.OrganizationId,
                request.ProjectId,
                report.Id,
                string.IsNullOrEmpty(fileName) ? "screenshot.png" : fileName,
                request.ScreenshotKey,
                0,
                "image/png"
            );
            dbContext.Assets.Add(screenshotAsset);
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("CreateReportHandler: Publishing ReportCreatedEvent for report {Id} (Org: {OrgId})",
            report.Id, request.OrganizationId);

        // 5. Publish Event to trigger analysis chain
        await publishEndpoint.Publish(new ReportCreatedEvent
        {
            ReportId = report.Id,
            CurrentOrganizationId = request.OrganizationId,
            ProjectId = request.ProjectId,
            Title = report.Title,
            Message = report.Message,
            StackTrace = report.StackTrace,
            CreatedAt = report.CreatedAt,
            Metadata = report.Metadata
        }, ct);

        return report.Id;
    }
}
