using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Assets.ValueObjects;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Infrastructure.Scheduling;

/// <summary>
/// A periodic background job that sweeps for "orphaned" assets stuck in a Pending state.
/// This typically happens when the Bouncer service crashes or hits a poison pill file.
/// </summary>
internal sealed class DatabaseSweeper(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseSweeper> logger) : BackgroundService
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _timeoutThreshold = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DatabaseSweeper: Starting background sweep process.");

        using var timer = new PeriodicTimer(_pollInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepOrphanedReportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DatabaseSweeper: Critical failure in sweep cycle.");
            }
        }
    }

    private async Task SweepOrphanedReportsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();

        var cutoffTime = DateTime.UtcNow.Subtract(_timeoutThreshold);

        logger.LogDebug("DatabaseSweeper: Checking for Assets stuck in Pending since {CutoffTime}", cutoffTime);

        // For Assets, we want to transition from Pending to Failed if they time out
        var updatedCount = await dbContext.Assets
            .Where(a => a.Status == AssetStatus.Pending && a.CreatedAt < cutoffTime)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, AssetStatus.Failed),
            ct);

        if (updatedCount > 0)
        {
            logger.LogWarning("DatabaseSweeper: Successfully marked {Count} orphaned assets as Failed (Processing Timeout).", updatedCount);
        }
        else
        {
            logger.LogDebug("DatabaseSweeper: No orphaned assets found.");
        }
    }
}
