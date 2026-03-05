using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Reports.GenerateSummary;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Scheduling;

internal sealed class CriticalMassSummaryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<CriticalMassSummaryJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
                var summaryService = scope.ServiceProvider.GetRequiredService<IClusterSummaryService>();

                logger.LogInformation("CriticalMassSummaryJob: Starting check for critical mass clusters.");

                var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);

                // Find clusters that:
                // 1. Are Open
                // 2. Haven't been summarized in the last 30 minutes (idempotency window)
                // 3. Have at least > 5 reports in the last hour

                // We only select the IDs here so we don't load all clusters into memory.
                var clusterIds = await db.Clusters
                    .AsNoTracking() // Important: Don't track here
                    .Where(c => c.Status == "Open" &&
                                (c.LastSummarizedAt == null || c.LastSummarizedAt <= thirtyMinutesAgo))
                    .Where(c => c.Reports.Count(r => r.CreatedAt >= oneHourAgo) > 5)
                    .Select(c => c.Id)
                    .ToListAsync(stoppingToken);

                if (clusterIds.Count > 0)
                {
                    logger.LogInformation("CriticalMassSummaryJob: Found {Count} clusters meeting criteria.", clusterIds.Count);
                }

                foreach (var clusterId in clusterIds)
                {
                    try
                    {
                        // Create a fresh scope per cluster to avoid DbContext bloat and ensure changes are saved independently.
                        using var innerScope = scopeFactory.CreateScope();
                        var innerDb = innerScope.ServiceProvider.GetRequiredService<WinnowDbContext>();
                        var innerSummaryService = innerScope.ServiceProvider.GetRequiredService<IClusterSummaryService>();

                        // Load the cluster we want to update
                        var cluster = await innerDb.Clusters.FindAsync([clusterId], stoppingToken);
                        if (cluster == null || cluster.Status != "Open") continue;

                        logger.LogInformation("CriticalMassSummaryJob: Summarizing cluster {ClusterId} due to critical mass.", cluster.Id);

                        var reportsForSummary = await innerDb.Reports
                            .Where(r => r.ClusterId == cluster.Id)
                            .OrderByDescending(r => r.CreatedAt)
                            .Take(50)
                            .ToListAsync(stoppingToken);

                        var result = await innerSummaryService.GenerateSummaryAsync(reportsForSummary, stoppingToken);

                        if (!result.IsError)
                        {
                            cluster.Title = result.Title;
                            cluster.Summary = result.Summary;
                            cluster.CriticalityScore = result.CriticalityScore;
                            cluster.CriticalityReasoning = result.CriticalityReasoning;
                            cluster.LastSummarizedAt = DateTime.UtcNow;

                            await innerDb.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "CriticalMassSummaryJob: Failed to summarize cluster {ClusterId}", clusterId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CriticalMassSummaryJob cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
