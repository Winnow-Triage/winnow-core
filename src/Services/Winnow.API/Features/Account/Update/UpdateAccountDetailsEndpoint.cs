using Winnow.API.Features.Account.GetDetails;
using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Account.Update;

public class UpdateAccountRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class UpdateAccountDetailsEndpoint(IMediator mediator)
    : Endpoint<UpdateAccountRequest, AccountDetailsResponse>
{
    public override void Configure()
    {
        Put("/account/me");
        Summary(s =>
        {
            s.Summary = "Update current user account details";
            s.Description = "Updates the profile information for the currently authenticated user, including name and email.";
        });
    }

    public override async Task HandleAsync(UpdateAccountRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var command = new UpdateAccountDetailsCommand
        {
            FullName = req.FullName,
            Email = req.Email,
            CurrentUserId = userId
        };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
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
