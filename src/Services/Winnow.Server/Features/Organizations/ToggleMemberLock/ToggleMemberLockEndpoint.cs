using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Organizations.ToggleMemberLock;

public sealed class ToggleMemberLockEndpoint(IMediator mediator)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Put("/organizations/{orgId}/members/{userId}/lock");
        Summary(s =>
        {
            s.Summary = "Toggle member lock status";
            s.Description = "Locks or restores access for a member in the organization.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var memberUserId = Route<string>("userId") ?? string.Empty;
        Console.WriteLine($"[LOCK] Attempting to toggle lock for user {memberUserId} in organization {orgIdRaw}");

        Guid orgId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId))
        {
            Console.WriteLine($"[LOCK] INVALID ORGID: {orgIdRaw}");
            AddError("Invalid organization context");
            ThrowIfAnyErrors();
        }

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var command = new ToggleMemberLockCommand(orgId, memberUserId, currentUserId);
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

        await Send.OkAsync(new { result.IsLocked }, ct);
    }
}
