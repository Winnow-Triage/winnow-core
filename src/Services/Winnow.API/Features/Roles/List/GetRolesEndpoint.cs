using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Winnow.API.Infrastructure.Security.Authorization;

namespace Winnow.API.Features.Roles.List;

[HttpGet("/organizations/{OrganizationId}/roles")]
[Authorize]
public sealed class GetRolesEndpoint(IMediator mediator) : Endpoint<GetRolesQuery, GetRolesResponse>
{
    public override async Task HandleAsync(GetRolesQuery req, CancellationToken ct)
    {
        var orgId = Route<Guid>("OrganizationId");
        var query = new GetRolesQuery(orgId);
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
