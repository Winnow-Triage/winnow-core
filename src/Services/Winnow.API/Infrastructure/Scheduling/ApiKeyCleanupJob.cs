using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Infrastructure.Scheduling;

internal sealed class ApiKeyCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ApiKeyCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("API Key Cleanup Job: Starting background process.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

                // Ignore the OrgId Global Query Filter to wipe expired keys across all tenants at once
                var expiredProjects = await db.Projects
                    .IgnoreQueryFilters()
                    .Where(p => p.SecondaryApiKeyExpiresAt != null && p.SecondaryApiKeyExpiresAt < DateTimeOffset.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expiredProjects.Count > 0)
                {
                    foreach (var project in expiredProjects)
                    {
                        project.RevokeSecondaryApiKey();
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("API Key Cleanup Job: Successfully scrubbed {Count} expired secondary API keys.", expiredProjects.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API Key Cleanup Job: Critical failure in cleanup cycle.");
            }

            // Run once per hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
