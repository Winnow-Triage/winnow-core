using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Users.ToggleLock;

public class ToggleUserLockRequest
{
    public string Id { get; set; } = string.Empty;
}

public class ToggleUserLockResponse
{
    public bool IsLocked { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ToggleUserLockEndpoint(IMediator mediator) : Endpoint<ToggleUserLockRequest, ToggleUserLockResponse>
{
    public override void Configure()
    {
        Post("/admin/users/{id}/toggle-lock");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Lock or unlock a user account (SuperAdmin only)";
            s.Description = "Toggles the account lockout status for a user. If locked, the user cannot log in.";
            s.Response<ToggleUserLockResponse>(200, "Status toggled successfully");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(ToggleUserLockRequest req, CancellationToken ct)
    {
        var command = new ToggleUserLockCommand { UserId = req.Id };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

