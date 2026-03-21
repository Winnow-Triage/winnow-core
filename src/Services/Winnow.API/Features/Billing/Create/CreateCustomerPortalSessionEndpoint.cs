using FastEndpoints;
using MediatR;
using Winnow.API.Infrastructure.MultiTenancy;

namespace Winnow.API.Features.Billing.Create;

public class PortalRequest
{
    /// <summary>Optional deep-link action: "update" or "cancel".</summary>
    public string? Action { get; set; }
}

public class PortalResponse
{
    public Uri PortalUrl { get; set; } = default!;
}

public sealed class CreateCustomerPortalSessionEndpoint(
    IMediator mediator,
    ITenantContext tenantContext)
    : Endpoint<PortalRequest, PortalResponse>
{
    public override void Configure()
    {
        Post("/billing/portal");
        DontThrowIfValidationFails();
    }

    public override async Task HandleAsync(PortalRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var command = new CreateCustomerPortalSessionCommand(tenantContext.CurrentOrganizationId.Value, req.Action);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(cancellation: ct);
                return;
            }

            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.OkAsync(new PortalResponse { PortalUrl = result.PortalUrl! }, ct);
    }
}