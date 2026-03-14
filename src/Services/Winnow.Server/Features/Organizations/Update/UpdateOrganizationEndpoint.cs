using Winnow.Server.Features.Organizations.Get;
using FastEndpoints;
using MediatR;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Features.Organizations.Update;

public class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateOrganizationEndpoint(
    IMediator mediator,
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

        var command = new UpdateOrganizationCommand(tenantContext.CurrentOrganizationId.Value, req.Name);
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

        await Send.OkAsync(result.Data!, cancellation: ct);
    }
}
