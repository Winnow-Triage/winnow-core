using MassTransit;
using Winnow.Contracts;
using Winnow.API.Extensions;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.Sanitize;



public sealed class AnalyzeReportConsumer(
    WinnowDbContext dbContext,
    IToxicityDetectionService toxicityService,
    IPiiRedactionService piiService,
    IPublishEndpoint publishEndpoint,
    ILogger<AnalyzeReportConsumer> logger) : IConsumer<ReportCreatedEvent>
{
    public async Task Consume(ConsumeContext<ReportCreatedEvent> context)
    {
        logger.LogInformation("AnalyzeReportConsumer: Starting analysis for report {Id} (Org: {OrgId})",
            context.Message.ReportId, context.Message.CurrentOrganizationId);

        var report = await dbContext.Reports.FindAsync([context.Message.ReportId], context.CancellationToken);

        if (report == null || string.IsNullOrWhiteSpace(report.Message))
        {
            logger.LogWarning("AnalyzeReportConsumer: Report {Id} or message content not found.", context.Message.ReportId);
            return;
        }

        // Gate 1: Toxicity Check
        var organization = await dbContext.Organizations.FindAsync([report.OrganizationId]);
        var result = await toxicityService.DetectToxicityAsync(report.Message, context.CancellationToken);

        // Fallback to default thresholds if settings are missing (safety check)
        var thresholds = organization?.Settings?.ToxicityLimits ?? ToxicityThresholds.Default;
        var policy = thresholds.ToPolicy();

        // The message is toxic if it violates ANY of the organization's customized thresholds
        if (result.Violates(policy))
        {
            report.MarkAsToxic();
            await dbContext.SaveChangesAsync(context.CancellationToken);

            // Short-circuit! Do NOT redact PII, do NOT send to clustering.
            return;
        }

        // Gate 2: PII Redaction (Only runs if it survived Gate 1)
        var redactedMessage = await piiService.RedactPiiAsync(report.Message, context.CancellationToken);
        report.UpdateMessage(redactedMessage);
        report.MarkAsClean();

        // Single database commit for the clean, redacted report
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // Gate 3: Hand off to your existing Vector/Clustering consumer
        await publishEndpoint.Publish(
            new ReportSanitizedEvent
            {
                ReportId = report.Id,
                CurrentOrganizationId = report.OrganizationId,
                ProjectId = report.ProjectId,
                Title = report.Title,
                Message = report.Message,
                StackTrace = report.StackTrace,
                CreatedAt = report.CreatedAt,
                Metadata = report.Metadata
            },
        context.CancellationToken);
    }
}