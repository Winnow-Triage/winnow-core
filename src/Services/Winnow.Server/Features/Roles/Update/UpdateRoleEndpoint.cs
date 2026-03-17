using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Roles.Update;

public record UpdateRoleRequest(string Name, List<Guid> PermissionIds);

[HttpPut("/organizations/{OrganizationId}/roles/{RoleId}")]
[Authorize]
public class UpdateRoleEndpoint(IMediator mediator) : Endpoint<UpdateRoleRequest, UpdateRoleResponse>
{
    public override async Task HandleAsync(UpdateRoleRequest req, CancellationToken ct)
    {
        var orgId = Route<Guid>("OrganizationId");
        var roleId = Route<Guid>("RoleId");

        var query = new UpdateRoleCommand(orgId, roleId, req.Name, req.PermissionIds);
        var result = await mediator.Send(query, ct);

        if (result.IsSuccess)
        {
            await Send.OkAsync(result.Data!, cancellation: ct);
        }
        else
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 400);
            return;
        }
    }
}
