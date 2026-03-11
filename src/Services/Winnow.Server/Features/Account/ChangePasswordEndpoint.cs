using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Clusters.ValueObjects;

namespace Winnow.Server.Features.Account;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordEndpoint(UserManager<ApplicationUser> userManager)
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

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
        {
            ThrowError(result.Errors.First().Description);
        }

        await Send.NoContentAsync(ct);
    }
}
