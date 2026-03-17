using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Roles.Create;

public record CreateRoleRequest(string Name, List<Guid> PermissionIds);

[HttpPost("/organizations/{OrganizationId}/roles")]
[Authorize]
public class CreateRoleEndpoint(IMediator mediator) : Endpoint<CreateRoleRequest, CreateRoleResponse>
{
    public override async Task HandleAsync(CreateRoleRequest req, CancellationToken ct)
    {
        var orgId = Route<Guid>("OrganizationId");

        var query = new CreateRoleCommand(orgId, req.Name, req.PermissionIds);
        var result = await mediator.Send(query, ct);

        if (result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 201;
            await Send.OkAsync(result.Data!, cancellation: ct);
        }
        else
        {
            await Send.ErrorsAsync(result.StatusCode ?? 400, cancellation: ct);
        }
    }
}
