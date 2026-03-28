using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Infrastructure.Scheduling;

internal sealed class InvitationCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<InvitationCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Invitation Cleanup Job: Starting background process.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

                // Ignore the OrgId Global Query Filter to wipe expired invites across all tenants at once
                int totalDeleted = await db.OrganizationInvitations
                    .IgnoreQueryFilters()
                    .Where(i => i.ExpiresAt < DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);

                if (totalDeleted > 0)
                {
                    logger.LogInformation("Invitation Cleanup Job: Successfully removed {Count} expired invitations.", totalDeleted);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invitation Cleanup Job: Critical failure in cleanup cycle.");
            }

            // Run once per hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}