using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard;

public class DashboardService(WinnowDbContext db) : IDashboardService
{
    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(CancellationToken ct)
    {
        // 1. Triage Metrics
        var totalReports = await db.Reports.CountAsync(ct);

        var activeClusters = await db.Reports
            .CountAsync(t => t.ParentReportId == null && t.Status != "Closed" && t.Status != "Duplicate", ct);

        var pendingReviews = await db.Reports
            .CountAsync(t => t.SuggestedParentId != null && t.Status != "Duplicate" && t.Status != "Closed", ct);

        double noiseRatio = totalReports > 0 
            ? 1.0 - ((double)activeClusters / totalReports) 
            : 0;

        int hoursSaved = (int)((totalReports - activeClusters) * 5.0 / 60.0);

        var triageMetrics = new TriageMetricsDto(
            totalReports,
            activeClusters,
            noiseRatio,
            pendingReviews,
            hoursSaved);

        // 2. Trending Clusters (Last 24 hours)
        var yesterday = DateTime.UtcNow.AddHours(-24);
        
        var trending = await db.Reports
            .Where(t => t.CreatedAt >= yesterday && t.ParentReportId != null)
            .GroupBy(t => t.ParentReportId)
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
            var clusterInfos = await db.Reports
                .Where(t => clusterIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Title, t.Status })
                .ToDictionaryAsync(t => t.Id, ct);
            
            var counts = await db.Reports
                .Where(t => clusterIds.Contains(t.ParentReportId) && t.ParentReportId != null)
                .GroupBy(t => t.ParentReportId)
                .Select(g => new { ClusterId = g.Key!.Value, Total = g.Count() })
                .ToDictionaryAsync(g => g.ClusterId, ct);

            foreach (var t in trending)
            {
                if (t.ClusterId.HasValue && clusterInfos.TryGetValue(t.ClusterId.Value, out var info))
                {
                    int total = counts.TryGetValue(t.ClusterId.Value, out var c) ? c.Total : 0;
                    total += 1; 

                    trendingDtos.Add(new TrendingClusterDto(
                        t.ClusterId.Value,
                        info.Title,
                        info.Status,
                        total,
                        t.Velocity,
                        t.Velocity > 10 
                    ));
                }
            }
        }

        // 3. Volume History (Bucketed by Hour)
        var historyRaw = await db.Reports
            .AsNoTracking()
            .Where(t => t.CreatedAt >= yesterday)
            .Select(t => new { t.CreatedAt, IsDuplicate = t.ParentReportId != null })
            .ToListAsync(ct);

        var grouped = historyRaw
            .GroupBy(t => new { t.CreatedAt.Date, t.CreatedAt.Hour })
            .ToDictionary(
                g => new DateTime(g.Key.Date.Year, g.Key.Date.Month, g.Key.Date.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                g => new { Unique = g.Count(x => !x.IsDuplicate), Duplicate = g.Count(x => x.IsDuplicate) }
            );

        var history = new List<VolumeMetricDto>();
        var now = DateTime.UtcNow;
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
