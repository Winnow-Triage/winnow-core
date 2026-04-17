using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Services.Emails;
using Microsoft.Extensions.Configuration;

namespace Winnow.API.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IConfiguration config) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var appUrl = config["AppUrl"] ?? throw new InvalidOperationException("AppUrl configuration is missing.");
            var resetUrl = $"{appUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

            await emailService.SendPasswordResetAsync(user.Email!, new Uri(resetUrl));
        }
    }
}
