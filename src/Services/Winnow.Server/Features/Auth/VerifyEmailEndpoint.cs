using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Auth;

public class VerifyEmailRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailResponse
{
    public string Message { get; set; } = string.Empty;
}

public sealed class VerifyEmailEndpoint(UserManager<ApplicationUser> userManager)
    : Endpoint<VerifyEmailRequest, VerifyEmailResponse>
{
    public override void Configure()
    {
        Get("/auth/verify-email");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Verify user email";
            s.Description = "Confirms a user's email address using a token sent via email.";
            s.Response<VerifyEmailResponse>(200, "Email verified successfully");
            s.Response(400, "Verification failed");
        });
    }

    public override async Task HandleAsync(VerifyEmailRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.UserId);
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Idempotency check: If user is already verified, return success immediately.
        // This avoids concurrency issues if the endpoint is called twice (e.g. React StrictMode).
        if (user.EmailConfirmed)
        {
            await Send.OkAsync(new VerifyEmailResponse
            {
                Message = "Email already verified. You can now log in."
            }, ct);
            return;
        }

        var result = await userManager.ConfirmEmailAsync(user, req.Token);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.OkAsync(new VerifyEmailResponse
        {
            Message = "Email verified successfully. You can now log in."
        }, ct);
    }
}
