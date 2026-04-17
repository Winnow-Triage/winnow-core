using Winnow.API.Features.Dashboard.Dtos;
using Winnow.API.Features.Dashboard.IService;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Features.Billing;
using Winnow.API.Features.Dashboard;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Dashboard.Service;

public class DashboardService(WinnowDbContext db) : IDashboardService
{
    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid organizationId, Guid? projectId = null, Guid? teamId = null, CancellationToken ct = default)
    {
        var baseQuery = await BuildBaseQueryAsync(organizationId, projectId, teamId, ct);

        var triageMetrics = await CalculateTriageMetricsAsync(organizationId, projectId, baseQuery, ct);
        var trendingClusters = await GetTrendingClustersAsync(baseQuery, ct);
        var volumeHistory = await GetVolumeHistoryAsync(baseQuery, ct);

        return new DashboardMetricsDto(triageMetrics, trendingClusters, volumeHistory);
    }

    private async Task<IQueryable<Domain.Reports.Report>> BuildBaseQueryAsync(Guid organizationId, Guid? projectId, Guid? teamId, CancellationToken ct)
    {
        var baseQuery = db.Reports.Where(r => r.OrganizationId == organizationId);

        if (projectId.HasValue)
        {
            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizationId == organizationId, ct)
                ?? throw new InvalidOperationException($"Project {projectId} not found in organization {organizationId}");

            if (teamId.HasValue && project.TeamId != teamId.Value)
            {
                throw new InvalidOperationException($"Project {projectId} does not belong to team {teamId}");
            }

            baseQuery = baseQuery.Where(r => r.ProjectId == projectId.Value);
        }
        else if (teamId.HasValue)
        {
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

        return baseQuery;
    }

    private async Task<TriageMetricsDto> CalculateTriageMetricsAsync(Guid organizationId, Guid? projectId, IQueryable<Domain.Reports.Report> baseQuery, CancellationToken ct)
    {
        var totalReports = await baseQuery.CountAsync(ct);

        var clusterQuery = db.Clusters.Where(c => c.OrganizationId == organizationId);
        if (projectId.HasValue)
        {
            clusterQuery = clusterQuery.Where(c => c.ProjectId == projectId.Value);
        }

        var activeClusters = await clusterQuery.CountAsync(c => c.Status == ClusterStatus.Open, ct);
        var unassignedReports = await baseQuery.CountAsync(r => r.ClusterId == null, ct);
        var uniqueIssues = activeClusters + unassignedReports;

        var pendingReportReviews = await baseQuery
            .Where(r => r.SuggestedClusterId != null && r.Status == ReportStatus.Open)
            .Join(db.Clusters.Where(c => c.ProjectId == projectId),
                r => r.SuggestedClusterId,
                c => c.Id,
                (r, c) => r.Id)
            .CountAsync(ct);

        var pendingClusterMerges = await clusterQuery
            .Where(c => c.SuggestedMergeClusterId != null && c.Status == ClusterStatus.Open)
            .Join(db.Clusters.Where(c => c.ProjectId == projectId),
                c1 => c1.SuggestedMergeClusterId,
                c2 => c2.Id,
                (c1, c2) => c1.Id)
            .CountAsync(ct);

        var pendingReviews = pendingReportReviews + pendingClusterMerges;
        double noiseRatio = totalReports > 0 ? 1.0 - ((double)uniqueIssues / totalReports) : 0;
        int hoursSaved = (int)((totalReports - uniqueIssues) * 5.0 / 60.0);

        return new TriageMetricsDto(totalReports, activeClusters, noiseRatio, pendingReviews, hoursSaved);
    }

    private async Task<List<TrendingClusterDto>> GetTrendingClustersAsync(IQueryable<Domain.Reports.Report> baseQuery, CancellationToken ct)
    {
        var yesterday = DateTime.UtcNow.AddHours(-24);

        var trending = await baseQuery
            .Where(r => r.CreatedAt >= yesterday && r.ClusterId != null)
            .GroupBy(t => t.ClusterId)
            .Select(g => new
            {
                ClusterId = g.Key!.Value,
                Velocity = g.Count()
            })
            .OrderByDescending(x => x.Velocity)
            .Take(5)
            .ToListAsync(ct);

        var trendingDtos = new List<TrendingClusterDto>();
        if (trending.Count > 0)
        {
            var clusterIds = trending.Select(t => t.ClusterId).ToList();
            var clusterInfos = await db.Clusters
                .Where(c => clusterIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Title, c.Status })
                .ToDictionaryAsync(c => c.Id, ct);

            var counts = await baseQuery
                .Where(r => r.ClusterId != null && clusterIds.Contains(r.ClusterId.Value))
                .GroupBy(t => t.ClusterId!.Value)
                .Select(g => new { ClusterId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(g => g.ClusterId, ct);

            foreach (var t in trending)
            {
                if (clusterInfos.TryGetValue(t.ClusterId, out var info))
                {
                    int total = counts.TryGetValue(t.ClusterId, out var c) ? c.Total : 0;

                    trendingDtos.Add(new TrendingClusterDto(
                        t.ClusterId,
                        info.Title ?? "Untitled Cluster",
                        info.Status.Name,
                        total,
                        t.Velocity,
                        t.Velocity > 10
                    ));
                }
            }
        }

        return trendingDtos;
    }

    private async Task<List<VolumeMetricDto>> GetVolumeHistoryAsync(IQueryable<Domain.Reports.Report> baseQuery, CancellationToken ct)
    {
        var yesterday = DateTime.UtcNow.AddHours(-24);

        var historyRaw = await baseQuery
            .AsNoTracking()
            .Where(r => r.CreatedAt >= yesterday)
            .Select(r => new
            {
#pragma warning disable EF1001
                r.CreatedAt,
                IsDuplicate = r.Status == ReportStatus.Dismissed ||
                              (r.ClusterId != null && db.Reports.Any(r2 => r2.ClusterId == r.ClusterId && r2.CreatedAt < r.CreatedAt))
#pragma warning restore EF1001
            })
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

        return history;
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
                // A report is "Unique" if it's NOT dismissed AND (it's unclustered OR it's the first in its cluster)
                UniqueCount = g.Count(r => r.Status != ReportStatus.Dismissed &&
                    (r.ClusterId == null || !db.Reports.Any(r2 => r2.ClusterId == r.ClusterId && r2.CreatedAt < r.CreatedAt)))
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
                monthData?.UniqueCount ?? 0
            ));
        }

        var org = await db.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.Plan })
            .FirstOrDefaultAsync(ct);

        var tierId = org?.Plan.Name?.ToLowerInvariant() ?? "free";
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

        // 2. Team Breakdown — count unique reports (non-duplicate)
        var teams = await db.Teams
            .Where(t => t.OrganizationId == organizationId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                ProjectCount = t.Projects.Count,
                ReportVolume = db.Reports.Count(r => r.OrganizationId == organizationId && db.Projects.Any(p => p.Id == r.ProjectId && p.TeamId == t.Id) && r.Status != ReportStatus.Dismissed && r.CreatedAt >= startOfMonth)
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
                ActiveClusters = db.Clusters.Count(c => c.ProjectId == g.Key && c.Status == ClusterStatus.Open)
            })
            .OrderByDescending(x => x.TotalReports)
            .Take(5)
            .ToListAsync(ct);

        var topProjectIds = topProjects.Select(p => p.ProjectId).ToList();
        var projectNames = await db.Projects
            .Where(p => topProjectIds.Contains(p.Id))
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

        // 1. Project Breakdown — count unique reports and active clusters
        var projects = await db.Projects
            .Where(p => p.TeamId == teamId && p.OrganizationId == organizationId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                ReportVolume = db.Reports.Count(r => r.ProjectId == p.Id && r.Status != ReportStatus.Dismissed && r.CreatedAt >= startOfMonth),
                ActiveClusters = db.Clusters.Count(c => c.ProjectId == p.Id && c.Status == ClusterStatus.Open)
            })
            .ToListAsync(ct);

        var projectBreakdownDtos = projects.Select(p => new ProjectBreakdownDto(p.Id, p.Name, p.ReportVolume, p.ActiveClusters)).ToList();

        // 2. Top Clusters (across all projects in the team) — query Clusters directly
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var topClusters = await db.Reports
            .Where(r => projectIds.Contains(r.ProjectId) && r.CreatedAt >= thirtyDaysAgo && r.ClusterId != null)
            .GroupBy(t => t.ClusterId!.Value)
            .Select(g => new
            {
                ClusterId = g.Key,
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
            var clusterInfos = await db.Clusters
                .Where(c => clusterIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Title, c.Status })
                .ToDictionaryAsync(c => c.Id, ct);

            foreach (var t in topClusters)
            {
                if (clusterInfos.TryGetValue(t.ClusterId, out var info))
                {
                    trendingDtos.Add(new TrendingClusterDto(
                        t.ClusterId,
                        info.Title ?? "Untitled Cluster",
                        info.Status.Name,
                        t.Total,
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
            .Select(r => new
            {
#pragma warning disable EF1001
                r.CreatedAt,
                IsDuplicate = r.Status == ReportStatus.Dismissed ||
                              (r.ClusterId != null && db.Reports.Any(r2 => r2.ClusterId == r.ClusterId && r2.CreatedAt < r.CreatedAt))
#pragma warning restore EF1001
            })
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
