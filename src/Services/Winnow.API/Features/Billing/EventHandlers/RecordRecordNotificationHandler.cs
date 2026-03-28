using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Reports.Events;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Billing.EventHandlers;

public class RecordRecordNotificationHandler(
    WinnowDbContext dbContext,
    ILogger<RecordRecordNotificationHandler> logger) : INotificationHandler<ReportCreatedEvent>
{
    public async Task Handle(ReportCreatedEvent notification, CancellationToken cancellationToken)
    {
        var msg = notification;

        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == msg.OrganizationId, cancellationToken);

        if (organization == null)
        {
            logger.LogWarning("Organization {Id} not found for billing update.", msg.OrganizationId);
            return;
        }

        organization.RecordReportUsage();

        logger.LogInformation("Incremented report usage for Organization {Id}.", msg.OrganizationId);
    }
}