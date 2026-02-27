using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Billing;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Dashboard;

public class DashboardService(WinnowDbContext db) : IDashboardService
{
    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid organizationId, Guid? projectId = null, Guid? teamId = null, CancellationToken ct = default)
    {
        // 1. Build Base Query
        var baseQuery = db.Reports
            .Where(r => r.OrganizationId == organizationId);

        if (projectId.HasValue)
        {
            // Verify project belongs to organization
            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId, ct);

            if (project == null)
            {
                throw new InvalidOperationException($"Project {projectId} not found in organization {organizationId}");
            }
            if (teamId.HasValue && project.TeamId != teamId.Value)
            {
                throw new InvalidOperationException($"Project {projectId} does not belong to team {teamId}");
            }

            baseQuery = baseQuery.Where(r => r.ProjectId == projectId.Value);
        }
        else if (teamId.HasValue)
        {
            // Verify team belongs to organization
            var teamExists = await db.Teams.AnyAsync(t => t.Id == teamId && t.OrganizationId == organizationId, ct);
            if (!teamExists)
            {
                throw new InvalidOperationException($"Team {teamId} not found in organization {organizationId}");
            }

            var projectIds = await db.Projects
                .Where(p => p.TeamId == teamId.Value && p.OrganizationId == organizationId)
                .Select(p => p.Id)
                .ToListAsync(ct);

            baseQuery = baseQuery.Where(r => projectIds.Contains(r.ProjectId));
        }

        // 2. Triage Metrics
        var totalReports = await baseQuery.CountAsync(ct);

        var activeClusters = await baseQuery
            .CountAsync(t => t.ParentReportId == null && t.Status != "Closed" && t.Status != "Duplicate", ct);

        var pendingReviews = await baseQuery
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

        // 3. Trending Clusters (Last 24 hours)
        var yesterday = DateTime.UtcNow.AddHours(-24);

        var trending = await baseQuery
            .Where(r => r.CreatedAt >= yesterday && r.ParentReportId != null)
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
            var clusterInfos = await baseQuery
                .Where(r => clusterIds.Contains(r.Id))
                .Select(t => new { t.Id, t.Title, t.Status })
                .ToDictionaryAsync(t => t.Id, ct);

            var counts = await baseQuery
                .Where(r => clusterIds.Contains(r.ParentReportId) && r.ParentReportId != null)
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

        // 4. Volume History (Bucketed by Hour)
        var historyRaw = await baseQuery
            .AsNoTracking()
            .Where(r => r.CreatedAt >= yesterday)
            .Select(r => new { r.CreatedAt, IsDuplicate = r.ParentReportId != null })
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

    public async Task<OrganizationDashboardDto> GetOrganizationDashboardAsync(Guid organizationId, CancellationToken ct = default)
    {
        // 1. Quota & Total Usage
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalUsage = await db.Reports
            .Where(r => r.OrganizationId == organizationId && r.CreatedAt >= startOfMonth)
            .CountAsync(ct);

        // Let's get the last 6 months (including current)
        var sixMonthsAgo = startOfMonth.AddMonths(-5);

        // Fetch reports for the last 6 months
        var monthlyCounts = await db.Reports
            .Where(r => r.OrganizationId == organizationId && r.CreatedAt >= sixMonthsAgo)
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                ReportCount = g.Count(),
                ClusterCount = g.Count(x => x.ParentReportId == null)
            })
            .ToListAsync(ct);

        var usageHistory = new List<MonthlyUsageDto>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = startOfMonth.AddMonths(-i);
            var monthData = monthlyCounts
                .FirstOrDefault(m => m.Year == targetMonth.Year && m.Month == targetMonth.Month);

            usageHistory.Add(new MonthlyUsageDto(
                targetMonth.ToString("MMM"),
                monthData?.ReportCount ?? 0,
                monthData?.ClusterCount ?? 0
            ));
        }

        var org = await db.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.SubscriptionTier })
            .FirstOrDefaultAsync(ct);

        var tierId = org?.SubscriptionTier?.ToLowerInvariant() ?? "free";
        int? limit = tierId switch
        {
            "free" => 50,
            "starter" => 500,
            "pro" => null,
            "enterprise" => null,
            _ => 50
        };

        var quota = new QuotaStatusDto(
            totalUsage,
            limit,
            limit, // We can just use the same limit for grace for now, or separate if needed
            limit.HasValue && totalUsage >= limit.Value,
            usageHistory);

        // 2. Team Breakdown
        var teams = await db.Teams
            .Where(t => t.OrganizationId == organizationId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                ProjectCount = t.Projects.Count,
                ReportVolume = db.Reports.Count(r => r.OrganizationId == organizationId && db.Projects.Any(p => p.Id == r.ProjectId && p.TeamId == t.Id) && r.ParentReportId == null && r.CreatedAt >= startOfMonth)
            })
            .ToListAsync(ct);

        var teamBreakdownDtos = teams.Select(t => new TeamBreakdownDto(t.Id, t.Name, t.ProjectCount, t.ReportVolume)).ToList();

        // 3. Top Projects (overall org)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var topProjects = await db.Reports
            .Where(r => r.OrganizationId == organizationId && r.CreatedAt >= thirtyDaysAgo)
            .GroupBy(r => r.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                TotalReports = g.Count(),
                ActiveClusters = g.Count(r => r.ParentReportId == null && r.Status != "Closed" && r.Status != "Duplicate")
            })
            .OrderByDescending(x => x.TotalReports)
            .Take(5)
            .ToListAsync(ct);

        var projectIds = topProjects.Select(p => p.ProjectId).ToList();
        var projectNames = await db.Projects
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var topProjectDtos = topProjects.Select(p => new TopProjectDto(
            p.ProjectId,
            projectNames.TryGetValue(p.ProjectId, out var name) ? name : "Unknown Project",
            p.TotalReports,
            p.ActiveClusters)).ToList();

        return new OrganizationDashboardDto(quota, teamBreakdownDtos, topProjectDtos);
    }

    public async Task<TeamDashboardDto> GetTeamDashboardAsync(Guid organizationId, Guid teamId, CancellationToken ct = default)
    {
        // Verify team belongs to organization
        var teamExists = await db.Teams.AnyAsync(t => t.Id == teamId && t.OrganizationId == organizationId, ct);
        if (!teamExists)
        {
            throw new InvalidOperationException($"Team {teamId} not found in organization {organizationId}");
        }

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var projectIds = await db.Projects
            .Where(p => p.TeamId == teamId && p.OrganizationId == organizationId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        // 1. Project Breakdown
        var projects = await db.Projects
            .Where(p => p.TeamId == teamId && p.OrganizationId == organizationId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                ReportVolume = db.Reports.Count(r => r.ProjectId == p.Id && r.ParentReportId == null && r.CreatedAt >= startOfMonth),
                ActiveClusters = db.Reports.Count(r => r.ProjectId == p.Id && r.ParentReportId == null && r.Status != "Closed" && r.Status != "Duplicate")
            })
            .ToListAsync(ct);

        var projectBreakdownDtos = projects.Select(p => new ProjectBreakdownDto(p.Id, p.Name, p.ReportVolume, p.ActiveClusters)).ToList();

        // 2. Top Clusters (across all projects in the team)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var topClusters = await db.Reports
            .Where(r => projectIds.Contains(r.ProjectId) && r.CreatedAt >= thirtyDaysAgo && r.ParentReportId != null)
            .GroupBy(t => t.ParentReportId)
            .Select(g => new
            {
                ClusterId = g.Key!.Value,
                Total = g.Count(),
                Velocity = g.Count(c => c.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync(ct);

        var trendingDtos = new List<TrendingClusterDto>();
        if (topClusters.Count > 0)
        {
            var clusterIds = topClusters.Select(t => t.ClusterId).ToList();
            var clusterInfos = await db.Reports
                .Where(r => clusterIds.Contains(r.Id))
                .Select(t => new { t.Id, t.Title, t.Status })
                .ToDictionaryAsync(t => t.Id, ct);

            foreach (var t in topClusters)
            {
                if (clusterInfos.TryGetValue(t.ClusterId, out var info))
                {
                    trendingDtos.Add(new TrendingClusterDto(
                        t.ClusterId,
                        info.Title,
                        info.Status,
                        t.Total + 1, // +1 for the parent report itself
                        t.Velocity,
                        t.Velocity > 10
                    ));
                }
            }
        }

        // 3. Volume History (Bucketed by Hour)
        var yesterday = DateTime.UtcNow.AddHours(-24);
        var historyRaw = await db.Reports
            .AsNoTracking()
            .Where(r => projectIds.Contains(r.ProjectId) && r.CreatedAt >= yesterday)
            .Select(r => new { r.CreatedAt, IsDuplicate = r.ParentReportId != null })
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

        return new TeamDashboardDto(projectBreakdownDtos, trendingDtos, history);
    }
}
