using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Organizations.RemoveMember;

public sealed class RemoveOrganizationMemberEndpoint(IMediator mediator)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/organizations/{orgId}/members/{userId}");
        Summary(s =>
        {
            s.Summary = "Remove a member from the organization";
            s.Description = "Removes the user from the organization members list. Cannot remove the last owner.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var memberUserId = Route<string>("userId") ?? string.Empty;
        Console.WriteLine($"[REMOVE] Attempting to remove user {memberUserId} from organization {orgIdRaw}");

        Guid orgId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId))
        {
            Console.WriteLine($"[REMOVE] INVALID ORGID: {orgIdRaw}");
            AddError("Invalid organization context");
            ThrowIfAnyErrors(400);
        }

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var command = new RemoveOrganizationMemberCommand(orgId, memberUserId, currentUserId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
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
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
