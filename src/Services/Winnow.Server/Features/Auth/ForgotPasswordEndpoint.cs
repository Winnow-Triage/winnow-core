using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Entities;
using Winnow.Server.Features.Auth;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Auth;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class ForgotPasswordEndpoint(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService) : Endpoint<ForgotPasswordRequest>
{
    public override void Configure()
    {
        Post("/auth/forgot-password");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Request a password reset email";
            s.Description = "Generates a password reset token and sends it to the user's email.";
            s.Response(200, "If the email exists, a reset link will be sent.");
            s.Response(400, "Validation failed");
        });
    }

    public override async Task HandleAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);

        // Security best practice: Don't reveal if the user exists
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Build the reset URL (frontend page)
            // In a real app, this would be a config setting
            var resetUrl = $"http://localhost:5173/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

            await emailService.SendPasswordResetAsync(user.Email!, new Uri(resetUrl));
        }

        await Send.OkAsync(new { Message = "If an account with that email exists, we have sent a password reset link." }, ct);
    }
}
