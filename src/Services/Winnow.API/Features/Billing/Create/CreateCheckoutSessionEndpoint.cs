using FastEndpoints;
using MediatR;
using Winnow.API.Infrastructure.MultiTenancy;

namespace Winnow.API.Features.Billing.Create;

public class CheckoutRequest
{
    public string TargetTier { get; set; } = string.Empty;
}

public class CheckoutResponse
{
    public Uri CheckoutUrl { get; set; } = default!;
}

public sealed class CreateCheckoutSessionEndpoint(
    IMediator mediator,
    ITenantContext tenantContext)
    : Endpoint<CheckoutRequest, CheckoutResponse>
{
    public override void Configure()
    {
        Post("/billing/checkout");
        // Endpoint requires authentication by default since AllowAnonymous() is NOT called.
    }

    public override async Task HandleAsync(CheckoutRequest req, CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        var command = new CreateCheckoutSessionCommand(tenantContext.CurrentOrganizationId.Value, req.TargetTier);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(cancellation: ct);
                return;
            }
            if (result.StatusCode == 400 && result.ErrorMessage != null && result.ErrorMessage.StartsWith("Invalid"))
            {
                AddError(r => r.TargetTier, result.ErrorMessage);
                await Send.ResponseAsync(new CheckoutResponse { }, 400, cancellation: ct);
                return;
            }

            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        await Send.OkAsync(new CheckoutResponse { CheckoutUrl = result.CheckoutUrl! }, cancellation: ct);
    }
}
