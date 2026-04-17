using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Services.Discord;
using Winnow.API.Services.Emails;

namespace Winnow.API.Features.Auth.Register;

public class UserRegisteredNotificationHandler(
    UserManager<ApplicationUser> userManager,
    IConfiguration config,
    IEmailService emailService,
    IInternalOpsNotifier internalOpsNotifier) : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var user = notification.User;

        try
        {
            await internalOpsNotifier.NotifyNewSignupAsync(user.Email!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[REGISTER] Internal notification failed: {ex.Message}");
        }

        try
        {
            Console.WriteLine($"[REGISTER] Sending welcome email to {user.Email}");
            await emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
            Console.WriteLine($"[REGISTER] Welcome email sent to {user.Email}");

            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var appUrl = config["AppUrl"] ?? "https://app.winnowtriage.com";
            var verificationUrl = $"{appUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

            Console.WriteLine($"[REGISTER] Sending verification email to {user.Email}");
            await emailService.SendEmailVerificationAsync(user.Email!, new Uri(verificationUrl));
            Console.WriteLine($"[REGISTER] Verification email sent to {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[REGISTER] FAILED to send emails to {user.Email}: {ex.Message}");
        }
    }
}
