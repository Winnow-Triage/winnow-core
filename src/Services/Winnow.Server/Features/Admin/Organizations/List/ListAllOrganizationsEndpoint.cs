using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Organizations.List;

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
public sealed class ListAllOrganizationsEndpoint(IMediator mediator) : Endpoint<EmptyRequest, List<OrganizationSummaryResponse>>
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
        var result = await mediator.Send(new ListAllOrganizationsQuery(), ct);
        await Send.OkAsync(result, ct);
    }
}