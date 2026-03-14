using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Infrastructure.Identity;

namespace Winnow.Server.Features.Auth.VerifyEmail;

public record VerifyEmailCommand(string UserId, string Token) : IRequest<VerifyEmailResult>;

public record VerifyEmailResult(bool IsSuccess, string Message, IEnumerable<string> Errors);

public class VerifyEmailHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    public async Task<VerifyEmailResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new VerifyEmailResult(false, "User not found.", ["User not found."]);
        }

        if (user.EmailConfirmed)
        {
            return new VerifyEmailResult(true, "Email already verified. You can now log in.", []);
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return new VerifyEmailResult(false, "Verification failed.", result.Errors.Select(e => e.Description));
        }

        return new VerifyEmailResult(true, "Email verified successfully. You can now log in.", []);
    }
}
