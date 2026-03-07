using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class UpdateOrganizationStatusRequest
{
    public Guid Id { get; set; }
    public bool IsSuspended { get; set; }
}

/// <summary>
/// Admin endpoint to suspend or activate an organization.
/// </summary>
public sealed class UpdateOrganizationStatusEndpoint(WinnowDbContext dbContext) : Endpoint<UpdateOrganizationStatusRequest>
{
    public override void Configure()
    {
        Patch("/admin/organizations/{Id}/status");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Update organization status (SuperAdmin only)";
            s.Description = "Suspends or activates an organization. Suspended organizations cannot access the API.";
            s.Response(200, "Success");
            s.Response(404, "Organization not found");
        });
    }

    public override async Task HandleAsync(UpdateOrganizationStatusRequest req, CancellationToken ct)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == req.Id, ct);

        if (org == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        org.Suspend("Unknown Reasoning"); // TODO: update to support a reasoning parameter in the request
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(cancellation: ct);
    }
}
