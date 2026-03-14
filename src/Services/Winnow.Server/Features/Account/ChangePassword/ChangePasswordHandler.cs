using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Infrastructure.Identity;

namespace Winnow.Server.Features.Account.ChangePassword;

public record ChangePasswordCommand : IRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string CurrentUserId { get; init; } = string.Empty;
}

public class ChangePasswordHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.CurrentUserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Errors.First().Description);
        }
    }
}
