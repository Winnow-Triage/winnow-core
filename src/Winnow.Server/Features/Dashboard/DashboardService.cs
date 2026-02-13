using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard;

public class DashboardService(WinnowDbContext db) : IDashboardService
{
    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken ct)
    {
        // 1. Triage Metrics
        // Total Tickets
        var totalTickets = await db.Tickets.CountAsync(ct);

        // Active Clusters (Roots that have children OR are marked as Open/Active)
        // A cluster leader is a ticket with ParentTicketId == null.
        // But "Active Cluster" usually means a leader that HAS children (duplicates).
        // Let's define "Active Clusters" as unique issues (Leaders).
        var activeClusters = await db.Tickets
            .CountAsync(t => t.ParentTicketId == null && t.Status != "Closed" && t.Status != "Duplicate", ct);

        // Pending Reviews (Suggestions)
        // Tickets that have a suggested parent but are NOT yet assigned/resolved
        var pendingReviews = await db.Tickets
            .CountAsync(t => t.SuggestedParentId != null && t.Status != "Duplicate" && t.Status != "Closed", ct);

        // Noise Ratio
        double noiseRatio = totalTickets > 0 
            ? 1.0 - ((double)activeClusters / totalTickets) 
            : 0;

        // Time Saved: (Total - Active) * 5 minutes / 60
        int hoursSaved = (int)((totalTickets - activeClusters) * 5.0 / 60.0);

        var triageMetrics = new TriageMetricsDto(
            totalTickets,
            activeClusters,
            noiseRatio,
            pendingReviews,
            hoursSaved);

        // 2. Trending Clusters (Last 24 hours)
        var yesterday = DateTime.UtcNow.AddHours(-24);
        
        // Find clusters that have received the most duplicates in the last 24h
        var trending = await db.Tickets
            .Where(t => t.CreatedAt >= yesterday && t.ParentTicketId != null)
            .GroupBy(t => t.ParentTicketId)
            .Select(g => new 
            {
                ClusterId = g.Key,
                Velocity = g.Count()
            })
            .OrderByDescending(x => x.Velocity)
            .Take(5)
            .ToListAsync(ct);

        var trendingDtos = new List<TrendingClusterDto>();
        if (trending.Count > 0)
        {
            var clusterIds = trending.Select(t => t.ClusterId).ToList();
            var clusterInfos = await db.Tickets
                .Where(t => clusterIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Title, t.Status })
                .ToDictionaryAsync(t => t.Id, ct);
            
            // Also get total counts for these clusters
            // This might be expensive if table is huge, but for top 5 it's okay-ish to run separate inputs or join
            // Let's do a fast count
            var counts = await db.Tickets
                .Where(t => clusterIds.Contains(t.ParentTicketId) && t.ParentTicketId != null)
                .GroupBy(t => t.ParentTicketId)
                .Select(g => new { ClusterId = g.Key!.Value, Total = g.Count() })
                .ToDictionaryAsync(g => g.ClusterId, ct);

            foreach (var t in trending)
            {
                if (t.ClusterId.HasValue && clusterInfos.TryGetValue(t.ClusterId.Value, out var info))
                {
                    int total = counts.TryGetValue(t.ClusterId.Value, out var c) ? c.Total : 0;
                    // Add +1 for the leader itself
                    total += 1; 

                    trendingDtos.Add(new TrendingClusterDto(
                        t.ClusterId.Value,
                        info.Title,
                        info.Status,
                        total,
                        t.Velocity,
                        t.Velocity > 10 // Hot threshold
                    ));
                }
            }
        }

        // 3. Volume History (Bucketed by Hour for last 24h)
        // Group raw data in memory
        var historyRaw = await db.Tickets
            .AsNoTracking()
            .Where(t => t.CreatedAt >= yesterday)
            .Select(t => new { t.CreatedAt, IsDuplicate = t.ParentTicketId != null })
            .ToListAsync(ct);

        var grouped = historyRaw
            .GroupBy(t => new { t.CreatedAt.Date, t.CreatedAt.Hour })
            .ToDictionary(
                g => new DateTime(g.Key.Date.Year, g.Key.Date.Month, g.Key.Date.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                g => new { Unique = g.Count(x => !x.IsDuplicate), Duplicate = g.Count(x => x.IsDuplicate) }
            );

        // Zero-fill for the last 24 hours
        var history = new List<VolumeMetricDto>();
        var now = DateTime.UtcNow;
        // Floor to current hour
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        
        for (int i = 23; i >= 0; i--)
        {
            var timeSlot = currentHour.AddHours(-i);
            if (grouped.TryGetValue(timeSlot, out var data))
            {
                history.Add(new VolumeMetricDto(timeSlot, data.Unique, data.Duplicate));
            }
            else
            {
                history.Add(new VolumeMetricDto(timeSlot, 0, 0));
            }
        }

        return new DashboardMetricsDto(triageMetrics, trendingDtos, history);
    }
}
