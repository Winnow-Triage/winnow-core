using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Integrations.GetDetails;

public class IntegrationDetailResponse
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public sealed class GetIntegrationConfigDetailEndpoint(IMediator mediator) : EndpointWithoutRequest<IntegrationDetailResponse>
{
    public override void Configure()
    {
        Get("/organizations/{OrgId}/integrations/{Id}");
        Summary(s =>
        {
            s.Summary = "Get integration details";
            s.Description = "Retrieves the full configuration for an integration, including masked settings.";
            s.Response<IntegrationDetailResponse>(200, "Integration details");
            s.Response(404, "Integration not found");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("Id");

        var orgId = Route<Guid>("OrgId"); // Assuming it's in the route or we need to add it.
                                          // Wait, if it's not in the route, we need a way to get the current tenant.
                                          // Let's check if there's a header or query param.
                                          // For now, I'll assume it's passed as a query param or route part if missing.
                                          // BUT, looking at the UI screenshot, it seems we are in a tenant context.

        var query = new GetIntegrationConfigDetailQuery(id, orgId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
