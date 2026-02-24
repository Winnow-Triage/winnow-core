using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Account;

public class UpdateAccountRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class UpdateAccountDetailsEndpoint(UserManager<ApplicationUser> userManager)
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

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.FullName))
        {
            user.FullName = req.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(req.Email) && !req.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await userManager.FindByEmailAsync(req.Email.Trim());
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ThrowError("Email is already in use by another account.");
            }

            user.Email = req.Email.Trim();
            user.UserName = req.Email.Trim(); // Sync Email and UserName
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ThrowError(result.Errors.First().Description);
        }

        await Send.OkAsync(new AccountDetailsResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        }, ct);
    }
}
