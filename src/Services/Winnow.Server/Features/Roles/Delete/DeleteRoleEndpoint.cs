using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Roles.Delete;

[HttpDelete("/organizations/{OrganizationId}/roles/{RoleId}")]
[Authorize]
public class DeleteRoleEndpoint(IMediator mediator) : Endpoint<DeleteRoleCommand, DeleteRoleResult>
{
    public override async Task HandleAsync(DeleteRoleCommand req, CancellationToken ct)
    {
        // Path parameters are bound automatically to the properties of DeleteRoleCommand
        // because FastEndpoints matches route names {OrganizationId} {RoleId} to property names
        var result = await mediator.Send(req, ct);

        if (result.IsSuccess)
        {
            await Send.NoContentAsync(cancellation: ct);
        }
        else
        {
            await Send.ErrorsAsync(result.StatusCode ?? 400, cancellation: ct);
        }
    }
}
