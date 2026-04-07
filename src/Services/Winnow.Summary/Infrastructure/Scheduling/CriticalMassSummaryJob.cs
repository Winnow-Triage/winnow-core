using System.Diagnostics.CodeAnalysis;
using global::Winnow.API.Domain.Clusters.ValueObjects;
using global::Winnow.API.Infrastructure.Persistence;
using global::Winnow.Contracts;
using Wolverine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Winnow.Summary.Infrastructure.Scheduling;

internal sealed class CriticalMassSummaryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<CriticalMassSummaryJob> logger) : BackgroundService
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Background service loop must continue on failure")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reduce frequency since we have reactive triggers now. This is a safety net.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WinnowDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                logger.LogInformation("CriticalMassSummaryJob: Starting safety net check for clusters needing summary.");

                var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);

                // Find clusters that:
                // 1. Are Open
                // 2. Haven't been summarized in the last 30 minutes
                // 3. Have at least >= 5 reports in the last hour
                var clusterIds = await db.Clusters
                    .AsNoTracking()
                    .Where(c => c.Status == ClusterStatus.Open &&
                                (c.LastSummarizedAt == null || c.LastSummarizedAt <= thirtyMinutesAgo))
                    .Where(c => db.Reports.Count(r => r.ClusterId == c.Id && r.CreatedAt >= oneHourAgo) >= 5)
                    .Select(c => new { c.Id, c.OrganizationId, c.ProjectId })
                    .ToListAsync(stoppingToken);

                if (clusterIds.Count > 0)
                {
                    logger.LogInformation("CriticalMassSummaryJob: Found {Count} clusters needing summary. Publishing events.", clusterIds.Count);
                }

                foreach (var info in clusterIds)
                {
                    await bus.PublishAsync(new GenerateClusterSummaryEvent(
                        info.Id,
                        info.OrganizationId,
                        info.ProjectId));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CriticalMassSummaryJob: Safety net check failed.");
            }

            // Run once every 10 minutes
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
