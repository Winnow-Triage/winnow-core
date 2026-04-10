using Microsoft.EntityFrameworkCore;
using Wolverine;
using Winnow.Contracts;
using Winnow.API.Extensions;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Infrastructure.Analysis;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.Sanitize;



public sealed class AnalyzeReportHandler(
    WinnowDbContext dbContext,
    IToxicityDetectionService toxicityService,
    IPiiRedactionService piiService,
    IMessageBus bus,
    ILogger<AnalyzeReportHandler> logger)
{
    public async Task Handle(ReportCreatedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("AnalyzeReportConsumer: Starting analysis for report {Id} (Org: {OrgId})",
            message.ReportId, message.CurrentOrganizationId);

        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == message.ReportId, cancellationToken);

        if (report == null || string.IsNullOrWhiteSpace(report.Message))
        {
            logger.LogWarning("AnalyzeReportConsumer: Report {Id} or message content not found.", message.ReportId);
            return;
        }

        // Gate 1: Toxicity Check
        var organization = await dbContext.Organizations.FindAsync([report.OrganizationId], cancellationToken);
        var result = await toxicityService.DetectToxicityAsync(report.Message, cancellationToken);

        // Fallback to default thresholds if settings are missing (safety check)
        var thresholds = organization?.Settings?.ToxicityLimits ?? ToxicityThresholds.Default;
        var policy = thresholds.ToPolicy();

        // The message is toxic if it violates ANY of the organization's customized thresholds
        if (result.Violates(policy))
        {
            report.MarkAsToxic();
            await dbContext.SaveChangesAsync(cancellationToken);

            // Short-circuit! Do NOT redact PII, do NOT send to clustering.
            return;
        }

        // Gate 2: PII Redaction (Only runs if it survived Gate 1)
        var redactedMessage = await piiService.RedactPiiAsync(report.Message, cancellationToken);
        report.UpdateMessage(redactedMessage);
        report.MarkAsClean();

        // Single database commit for the clean, redacted report
        await dbContext.SaveChangesAsync(cancellationToken);

        // Gate 3: Hand off to your existing Vector/Clustering consumer
        await bus.PublishAsync(
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
            });
    }
}