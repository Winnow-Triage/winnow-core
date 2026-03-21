using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Account.ChangePassword;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordEndpoint(IMediator mediator)
    : Endpoint<ChangePasswordRequest>
{
    public override void Configure()
    {
        Post("/account/change-password");
        Summary(s =>
        {
            s.Summary = "Change current user password";
            s.Description = "Changes the password for the currently authenticated user.";
        });
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var command = new ChangePasswordCommand
        {
            CurrentPassword = req.CurrentPassword,
            NewPassword = req.NewPassword,
            CurrentUserId = userId
        };

        try
        {
            await mediator.Send(command, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "User not found.")
            {
                await Send.NotFoundAsync(ct);
            }
            else
            {
                ThrowError(ex.Message);
            }
        }
    }
}
