using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Services.Emails;

namespace Winnow.Server.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var resetUrl = $"http://localhost:5173/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

            await emailService.SendPasswordResetAsync(user.Email!, new Uri(resetUrl));
        }
    }
}
