using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Auth;

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ResetPasswordEndpoint(UserManager<ApplicationUser> userManager) : Endpoint<ResetPasswordRequest>
{
    public override void Configure()
    {
        Post("/auth/reset-password");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Reset a user's password using a token";
            s.Description = "Validates the reset token and updates the user's password.";
            s.Response(200, "Password reset successful");
            s.Response(400, "Invalid token or request");
        });
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            // Security: Don't reveal if the user exists, but here the user already has the token
            ThrowError("Invalid request");
        }

        var result = await userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            ThrowIfAnyErrors();
        }

        await Send.OkAsync(new { Message = "Your password has been reset successfully." }, ct);
    }
}
