using FastEndpoints;
using MediatR;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Organizations.Get;

public class CurrentOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class GetCurrentOrganizationEndpoint(
    IMediator mediator,
    ITenantContext tenantContext)
    : EndpointWithoutRequest<CurrentOrganizationResponse>
{
    public override void Configure()
    {
        Get("/organizations/current");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        var query = new GetCurrentOrganizationQuery(tenantContext.CurrentOrganizationId.Value);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(cancellation: ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, cancellation: ct);
    }
}
