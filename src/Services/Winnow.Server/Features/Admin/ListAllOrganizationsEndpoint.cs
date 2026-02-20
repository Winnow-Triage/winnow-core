using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;



/// <summary>
/// Response DTO for an organization summary.
/// </summary>
public class OrganizationSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TeamCount { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
}

/// <summary>
/// Admin endpoint to list all organizations, bypassing tenant isolation.
/// </summary>
public class ListAllOrganizationsEndpoint(WinnowDbContext dbContext) : Endpoint<EmptyRequest, List<OrganizationSummaryResponse>>
{
    public override void Configure()
    {
        Get("/admin/organizations");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "List all organizations (SuperAdmin only)";
            s.Description = "Returns a list of all organizations with team counts and subscription tiers, bypassing tenant isolation.";
            s.Response<List<OrganizationSummaryResponse>>(200, "Success");
            s.Response(401, "Unauthorized (missing or invalid JWT)");
            s.Response(403, "Forbidden (user is not SuperAdmin)");
        });
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        // Must ignore global query filters to see all organizations
        var organizations = await dbContext.Organizations
            .IgnoreQueryFilters()
            .Include(o => o.Teams)
            .ThenInclude(t => t.Projects)
            .Include(o => o.Members)
            .ToListAsync(ct);

        var result = organizations.Select(o => new OrganizationSummaryResponse
        {
            Id = o.Id,
            Name = o.Name,
            StripeCustomerId = o.StripeCustomerId,
            SubscriptionTier = o.SubscriptionTier,
            CreatedAt = o.CreatedAt,
            TeamCount = o.Teams.Count,
            MemberCount = o.Members.Count,
            ProjectCount = o.Teams.Sum(t => t.Projects.Count)
        }).ToList();

        await Send.OkAsync(result, ct);
    }
}