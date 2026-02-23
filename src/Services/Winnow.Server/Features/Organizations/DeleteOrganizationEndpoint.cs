using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

public class DeleteOrganizationEndpoint(
    WinnowDbContext db,
    ITenantContext tenantContext)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/organizations/current");
        Summary(s =>
        {
            s.Summary = "Delete Current Organization";
            s.Description = "Permanently deletes the currently active organization and all its data.";
            s.Response(204, "Organization deleted successfully");
            s.Response(400, "Invalid request");
            s.Response(404, "Organization not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == tenantContext.CurrentOrganizationId.Value, ct);

        if (organization == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        db.Organizations.Remove(organization);

        await db.SaveChangesAsync(ct);

        await Send.NoContentAsync(cancellation: ct);
    }
}
