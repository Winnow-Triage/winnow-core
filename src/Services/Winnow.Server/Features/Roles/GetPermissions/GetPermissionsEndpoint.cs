using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Roles.GetPermissions;

[HttpGet("/organizations/{OrganizationId}/permissions")]
[Authorize]
public class GetPermissionsEndpoint(IMediator mediator) : Endpoint<GetPermissionsQuery, GetPermissionsResponse>
{
    public override async Task HandleAsync(GetPermissionsQuery req, CancellationToken ct)
    {
        var orgId = Route<Guid>("OrganizationId");
        var query = new GetPermissionsQuery(orgId);
        var result = await mediator.Send(query, ct);

        if (result.IsSuccess)
        {
            await Send.OkAsync(result.Data!, cancellation: ct);
        }
        else
        {
            await Send.ErrorsAsync(result.StatusCode ?? 400, cancellation: ct);
        }
    }
}
