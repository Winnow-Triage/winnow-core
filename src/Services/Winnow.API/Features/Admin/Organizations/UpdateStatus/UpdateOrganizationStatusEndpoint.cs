using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Admin.Organizations.UpdateStatus;

public class UpdateOrganizationStatusRequest
{
    public Guid Id { get; set; }
    public bool IsSuspended { get; set; }
}

/// <summary>
/// Admin endpoint to suspend or activate an organization.
/// </summary>
public sealed class UpdateOrganizationStatusEndpoint(IMediator mediator) : Endpoint<UpdateOrganizationStatusRequest>
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
        var command = new UpdateOrganizationStatusCommand
        {
            Id = req.Id,
            IsSuspended = req.IsSuspended
        };

        try
        {
            await mediator.Send(command, ct);
            await Send.OkAsync(cancellation: ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

