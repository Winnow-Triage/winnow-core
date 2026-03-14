using FastEndpoints;
using MediatR;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Organizations.Delete;

public sealed class DeleteOrganizationEndpoint(
    IMediator mediator,
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

        var command = new DeleteOrganizationCommand(tenantContext.CurrentOrganizationId.Value);
        var result = await mediator.Send(command, ct);

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

        await Send.NoContentAsync(cancellation: ct);
    }
}
