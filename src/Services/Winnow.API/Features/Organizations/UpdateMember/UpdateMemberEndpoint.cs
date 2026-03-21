using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Winnow.API.Infrastructure.Security.Authorization;

namespace Winnow.API.Features.Organizations.UpdateMember;

public record UpdateMemberRequest(Guid RoleId);

[HttpPut("/organizations/{OrganizationId}/members/{UserId}")]
[Authorize]
public sealed class UpdateMemberEndpoint(IMediator mediator) : Endpoint<UpdateMemberRequest, UpdateMemberResponse>
{
    public override async Task HandleAsync(UpdateMemberRequest req, CancellationToken ct)
    {
        var orgId = Route<Guid>("OrganizationId");
        var userId = Route<string>("UserId");

        var command = new UpdateMemberCommand(orgId, userId!, req.RoleId);
        var result = await mediator.Send(command, ct);

        if (result.IsSuccess)
        {
            await Send.OkAsync(result.Data!, cancellation: ct);
        }
        else
        {
            if (result.StatusCode == 403)
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 400);
        }
    }
}
