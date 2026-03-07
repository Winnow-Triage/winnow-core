using FastEndpoints;
using Microsoft.EntityFrameworkCore;
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
    public bool IsSuspended { get; set; }
    public int TeamCount { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
}

/// <summary>
/// Admin endpoint to list all organizations, bypassing tenant isolation.
/// </summary>
public sealed class ListAllOrganizationsEndpoint(WinnowDbContext dbContext) : Endpoint<EmptyRequest, List<OrganizationSummaryResponse>>
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
        // Execute the Select projection directly against the database
        var result = await dbContext.Organizations
            .IgnoreQueryFilters()
            .Select(o => new OrganizationSummaryResponse
            {
                Id = o.Id,
                Name = o.Name,
                // Safe unwrapping of your DDD Value Object for the SQL translation
                StripeCustomerId = o.BillingIdentity.HasValue ? o.BillingIdentity.Value.CustomerId : null,
                SubscriptionTier = o.Plan.Name,
                CreatedAt = o.CreatedAt,
                IsSuspended = o.IsSuspended,

                // EF Core translates these perfectly into SQL COUNT() sub-queries
                TeamCount = dbContext.Teams.IgnoreQueryFilters().Count(t => t.OrganizationId == o.Id),
                MemberCount = dbContext.OrganizationMembers.IgnoreQueryFilters().Count(m => m.OrganizationId == o.Id),
                ProjectCount = dbContext.Projects.IgnoreQueryFilters().Count(p => p.OrganizationId == o.Id)
            })
            .ToListAsync(ct);

        await Send.OkAsync(result, ct);
    }
}