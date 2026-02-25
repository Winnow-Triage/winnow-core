using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Winnow.Server.Entities;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Auth;

public sealed class ResendVerificationEndpoint(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IMemoryCache cache,
    IConfiguration config) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/resend-verification");
        Summary(s =>
        {
            s.Summary = "Resend email verification token";
            s.Description = "Generates a fresh email confirmation token and sends it to the authenticated user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
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

        if (user.EmailConfirmed)
        {
            await Send.OkAsync(new { Message = "Email already verified." }, ct);
            return;
        }

        var cacheKey = $"resend-verification-{user.Id}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            await Send.ErrorsAsync(429, ct); // Rate limit reached
            return;
        }

        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var verificationUrl = $"{config["AppUrl"]}/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

        await emailService.SendEmailVerificationAsync(user.Email!, new Uri(verificationUrl));

        // Set rate limit cache
        cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));

        await Send.OkAsync(new { Message = "Verification email sent successfully." }, ct);
    }
}
