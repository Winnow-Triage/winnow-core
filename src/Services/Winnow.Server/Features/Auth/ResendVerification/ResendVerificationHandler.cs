using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Auth.ResendVerification;

public record ResendVerificationCommand(string UserId) : IRequest<ResendVerificationResult>;

public record ResendVerificationResult(bool IsSuccess, string Message, int? StatusCode = null);

public class ResendVerificationHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IMemoryCache cache,
    IConfiguration config) : IRequestHandler<ResendVerificationCommand, ResendVerificationResult>
{
    public async Task<ResendVerificationResult> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new ResendVerificationResult(false, "User not found.", 404);
        }

        if (user.EmailConfirmed)
        {
            return new ResendVerificationResult(true, "Email already verified.", 200);
        }

        var cacheKey = $"resend-verification-{user.Id}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return new ResendVerificationResult(false, "Rate limit reached.", 429);
        }

        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var verificationUrl = $"{config["AppUrl"]}/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

        await emailService.SendEmailVerificationAsync(user.Email!, new Uri(verificationUrl));

        cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));

        return new ResendVerificationResult(true, "Verification email sent successfully.", 200);
    }
}
