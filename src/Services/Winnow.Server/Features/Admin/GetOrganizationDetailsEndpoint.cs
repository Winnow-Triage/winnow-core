using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

/// <summary>
/// Request DTO for getting organization details by ID.
/// </summary>
public class GetOrganizationDetailsRequest
{
    public Guid Id { get; set; }
}

/// <summary>
/// Detailed organization statistics.
/// </summary>
public class OrganizationDetailsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPaidTier { get; set; }

    // Counts
    public int TeamCount { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
    public int ReportCount { get; set; }
    public int AssetCount { get; set; }
    public int IntegrationCount { get; set; }

    // Recent activity (optional)
    public DateTime? LastReportDate { get; set; }
    public DateTime? LastMemberJoinDate { get; set; }

    // Team summaries
    public List<TeamSummary> Teams { get; set; } = new();
    public List<MemberSummary> Members { get; set; } = new();

    // Quotas
    public QuotaStatus Quota { get; set; } = new();
    public List<ProjectQuotaSummary> ProjectQuotas { get; set; } = new();
}

public class QuotaStatus
{
    public int BaseLimit { get; set; }
    public int GraceLimit { get; set; }
    public int MonthlyReportCount { get; set; }
    public bool IsOverage { get; set; }
    public bool IsLocked { get; set; }
    public int? AiSummaryLimit { get; set; }
    public int CurrentMonthAiSummaries { get; set; }
}

public class ProjectQuotaSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MonthlyReportCount { get; set; }
}

/// <summary>
/// Summary of a team within the organization.
/// </summary>
public class TeamSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ProjectCount { get; set; }
}

/// <summary>
/// Summary of a member within the organization.
/// </summary>
public class MemberSummary
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
}

/// <summary>
/// Admin endpoint to get detailed statistics for a specific organization.
/// </summary>
public sealed class GetOrganizationDetailsEndpoint(WinnowDbContext dbContext) : Endpoint<GetOrganizationDetailsRequest, OrganizationDetailsResponse>
{
    public override void Configure()
    {
        Get("/admin/organizations/{id}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Get detailed organization statistics (SuperAdmin only)";
            s.Description = "Returns detailed stats for a specific organization, including team, member, project, and report counts, bypassing tenant isolation.";
            s.Response<OrganizationDetailsResponse>(200, "Success");
            s.Response(404, "Organization not found");
            s.Response(401, "Unauthorized (missing or invalid JWT)");
            s.Response(403, "Forbidden (user is not SuperAdmin)");
        });
    }

    public override async Task HandleAsync(GetOrganizationDetailsRequest req, CancellationToken ct)
    {
        // Must ignore global query filters to see the organization regardless of tenant
        var organization = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (organization == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get additional counts

        var reportCount = await dbContext.Reports
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == req.Id)
            .CountAsync(ct);

        var assetCount = await dbContext.Assets
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == req.Id)
            .CountAsync(ct);

        var integrationCount = await dbContext.Integrations
            .IgnoreQueryFilters()
            .Where(i => i.OrganizationId == req.Id)
            .CountAsync(ct);

        var lastReportDate = await dbContext.Reports
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == req.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var lastMemberJoinDate = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .Where(m => m.OrganizationId == req.Id)
            .OrderByDescending(m => m.JoinedAt)
            .Select(m => (DateTime?)m.JoinedAt)
            .FirstOrDefaultAsync(ct);

        // Calculate quotas
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthlyReportCounts = await dbContext.Reports
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == req.Id && r.CreatedAt >= startOfMonth)
            .GroupBy(r => r.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, ct);

        int totalMonthlyReports = monthlyReportCounts.Values.Sum();

        int baseLimit = organization.Plan.Name.ToLowerInvariant() switch
        {
            "free" => 50,
            "starter" => 500,
            "pro" => int.MaxValue,
            "enterprise" => int.MaxValue,
            _ => 50
        };

        int graceLimit = organization.Plan.Name.ToLowerInvariant() switch
        {
            "free" => 100,
            "starter" => 1000,
            "pro" => int.MaxValue,
            "enterprise" => int.MaxValue,
            _ => 100
        };

        var effectiveAiLimit = organization.SummaryQuota.Limit;
        if (effectiveAiLimit == 0)
        {
            effectiveAiLimit = organization.Plan.Name.ToLowerInvariant() switch
            {
                "enterprise" => -1,
                "pro" => 500,
                "starter" => 50,
                _ => 0
            };
        }

        int? aiSummaryLimit = effectiveAiLimit == -1 ? null : effectiveAiLimit;

        var quotaStatus = new QuotaStatus
        {
            BaseLimit = baseLimit,
            GraceLimit = graceLimit,
            MonthlyReportCount = totalMonthlyReports,
            IsOverage = baseLimit != int.MaxValue && totalMonthlyReports >= baseLimit,
            IsLocked = graceLimit != int.MaxValue && totalMonthlyReports >= graceLimit,
            AiSummaryLimit = aiSummaryLimit,
            CurrentMonthAiSummaries = organization.SummaryQuota.Consumed
        };

        var projects = await dbContext.Projects
            .IgnoreQueryFilters()
            .Where(p => p.OrganizationId == req.Id)
            .ToListAsync(ct);

        var projectQuotas = projects
            .Select(p => new ProjectQuotaSummary
            {
                Id = p.Id,
                Name = p.Name,
                MonthlyReportCount = monthlyReportCounts.GetValueOrDefault(p.Id, 0)
            })
            .OrderByDescending(p => p.MonthlyReportCount)
            .ToList();

        var teams = await dbContext.Teams
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == req.Id)
            .ToListAsync(ct);

        var projectCountPerTeam = projects.Where(p => p.TeamId.HasValue).GroupBy(p => p.TeamId!.Value).ToDictionary(g => g.Key, g => g.Count());

        var membersList = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == req.Id)
            .ToListAsync(ct);

        var response = new OrganizationDetailsResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            StripeCustomerId = organization.BillingIdentity?.CustomerId,
            SubscriptionTier = organization.Plan.Name,
            CreatedAt = organization.CreatedAt,
            IsPaidTier = organization.Plan.Name == "Starter" || organization.Plan.Name == "Pro" || organization.Plan.Name == "Enterprise",
            TeamCount = organization.Teams.Count,
            MemberCount = organization.Members.Count,
            ProjectCount = organization.Projects.Count,
            ReportCount = reportCount,
            AssetCount = assetCount,
            IntegrationCount = integrationCount,
            LastReportDate = lastReportDate,
            LastMemberJoinDate = lastMemberJoinDate,
            Teams = teams.Select(t => new TeamSummary
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt,
                ProjectCount = projectCountPerTeam.GetValueOrDefault(t.Id, 0)
            }).ToList(),
            Members = membersList.Select(m => new MemberSummary
            {
                Id = m.Id,
                UserId = m.UserId,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                UserEmail = m.User?.Email,
                UserFullName = m.User?.FullName
            }).ToList(),
            Quota = quotaStatus,
            ProjectQuotas = projectQuotas
        };

        await Send.OkAsync(response, ct);
    }
}