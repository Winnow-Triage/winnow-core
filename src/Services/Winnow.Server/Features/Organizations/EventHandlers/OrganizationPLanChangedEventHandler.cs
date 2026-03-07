using MediatR;
using Winnow.Server.Domain.Organizations.Events;
using Winnow.Server.Services.Quota;

namespace Winnow.Server.Features.Organizations.EventHandlers;

internal sealed class OrganizationPlanChangedEventHandler(
    IQuotaService quotaService,
    ILogger<OrganizationPlanChangedEventHandler> logger)
    : INotificationHandler<OrganizationPlanUpgradedEvent>,
      INotificationHandler<OrganizationPlanDowngradedEvent>
{
    public async Task Handle(OrganizationPlanUpgradedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Organization {OrganizationId} upgraded. Reconciling quotas to unlock reports...", notification.OrganizationId);
        await ReconcileQuotasAsync(notification.OrganizationId, cancellationToken);
    }

    public async Task Handle(OrganizationPlanDowngradedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Organization {OrganizationId} downgraded. Reconciling quotas to enforce limits...", notification.OrganizationId);
        await ReconcileQuotasAsync(notification.OrganizationId, cancellationToken);
    }

    private async Task ReconcileQuotasAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        try
        {
            await quotaService.ResolveQuotaDiscrepanciesAsync(organizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reconcile quotas for Organization {OrganizationId}.", organizationId);
            throw;
        }
    }
}