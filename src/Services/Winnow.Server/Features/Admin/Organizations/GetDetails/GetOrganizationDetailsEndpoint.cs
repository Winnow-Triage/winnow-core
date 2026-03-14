using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Organizations.GetDetails;

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
public sealed class GetOrganizationDetailsEndpoint(IMediator mediator) : Endpoint<GetOrganizationDetailsRequest, OrganizationDetailsResponse>
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
        var query = new GetOrganizationDetailsQuery { Id = req.Id };

        try
        {
            var result = await mediator.Send(query, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}