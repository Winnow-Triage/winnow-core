using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Domain.Projects;
using Winnow.Server.Domain.Teams;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Organizations.GetDetails;

public record GetOrganizationDetailsQuery : IRequest<OrganizationDetailsResponse>
{
    public Guid Id { get; init; }
}

public class GetOrganizationDetailsHandler(
    IOrganizationRepository organizationRepo,
    IProjectRepository projectRepo,
    ITeamRepository teamRepo,
    WinnowDbContext dbContext) : IRequestHandler<GetOrganizationDetailsQuery, OrganizationDetailsResponse>
{
    public async Task<OrganizationDetailsResponse> Handle(GetOrganizationDetailsQuery request, CancellationToken cancellationToken)
    {
        // Must be able to see the organization regardless of tenant for Admin endpoints
        // GetByIdAsync using FindAsync bypasses global query filters in EF Core
        var organization = await organizationRepo.GetByIdAsync(request.Id, cancellationToken);

        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        // Use repositories for counts and recent activity where possible.
        // For complex joins/admin bypass, we still use dbContext selectively here.

        var reportCount = await dbContext.Reports
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == request.Id)
            .CountAsync(cancellationToken);

        var assetCount = await dbContext.Assets
            .IgnoreQueryFilters()
            .Where(a => a.OrganizationId == request.Id)
            .CountAsync(cancellationToken);

        var integrationCount = await dbContext.Integrations
            .IgnoreQueryFilters()
            .Where(i => i.OrganizationId == request.Id)
            .CountAsync(cancellationToken);

        var lastReportDate = await dbContext.Reports
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == request.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastMemberJoinDate = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .Where(m => m.OrganizationId == request.Id)
            .OrderByDescending(m => m.JoinedAt)
            .Select(m => (DateTime?)m.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Calculate quotas
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthlyReportCounts = await dbContext.Reports
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == request.Id && r.CreatedAt >= startOfMonth)
            .GroupBy(r => r.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, cancellationToken);

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

        // Use Project Repository
        var projects = await projectRepo.GetByOrganizationIdAsync(request.Id, cancellationToken);

        var projectQuotas = projects
            .Select(p => new ProjectQuotaSummary
            {
                Id = p.Id,
                Name = p.Name,
                MonthlyReportCount = monthlyReportCounts.GetValueOrDefault(p.Id, 0)
            })
            .OrderByDescending(p => p.MonthlyReportCount)
            .ToList();

        // Use Team Repository
        var teams = await teamRepo.GetByOrganizationIdAsync(request.Id, cancellationToken);

        var projectCountPerTeam = projects.Where(p => p.TeamId.HasValue).GroupBy(p => p.TeamId!.Value).ToDictionary(g => g.Key, g => g.Count());

        var membersList = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .Where(m => m.OrganizationId == request.Id)
            .Join(dbContext.Users.IgnoreQueryFilters(), m => m.UserId, u => u.Id, (m, u) => new MemberSummary
            {
                Id = m.Id,
                UserId = m.UserId,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                UserEmail = u.Email,
                UserFullName = u.FullName
            })
            .ToListAsync(cancellationToken);

        return new OrganizationDetailsResponse
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
            Members = membersList,
            Quota = quotaStatus,
            ProjectQuotas = projectQuotas
        };
    }
}
