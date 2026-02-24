using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

public class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateOrganizationEndpoint(
    WinnowDbContext db,
    ITenantContext tenantContext)
    : Endpoint<UpdateOrganizationRequest, CurrentOrganizationResponse>
{
    public override void Configure()
    {
        Put("/organizations/current");
        Summary(s =>
        {
            s.Summary = "Update Current Organization";
            s.Description = "Updates the name of the currently active organization.";
            s.Response<CurrentOrganizationResponse>(200, "Organization updated successfully");
            s.Response(400, "Invalid request");
            s.Response(404, "Organization not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(UpdateOrganizationRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            ThrowError("Organization name cannot be empty.", 400);
            return;
        }

        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == tenantContext.CurrentOrganizationId.Value, ct);

        if (organization == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        organization.Name = req.Name.Trim();

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new CurrentOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = string.IsNullOrEmpty(organization.SubscriptionTier) ? "Free" : organization.SubscriptionTier,
            CreatedAt = organization.CreatedAt
        }, cancellation: ct);
    }
}
