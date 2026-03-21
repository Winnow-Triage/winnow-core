using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ResetPasswordResult>;

public record ResetPasswordResult(bool IsSuccess, string Message, IEnumerable<string> Errors);

public class ResetPasswordHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new ResetPasswordResult(false, "Invalid request", ["Invalid request"]);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            return new ResetPasswordResult(false, "Invalid request", result.Errors.Select(e => e.Description));
        }

        return new ResetPasswordResult(true, "Your password has been reset successfully.", []);
    }
}
