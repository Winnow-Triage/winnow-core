using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Features.Admin.Users.ToggleLock;

public record ToggleUserLockCommand : IRequest<ToggleUserLockResponse>
{
    public string UserId { get; init; } = string.Empty;
}

public class ToggleUserLockHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<ToggleUserLockHandler> logger) : IRequestHandler<ToggleUserLockCommand, ToggleUserLockResponse>
{
    public async Task<ToggleUserLockResponse> Handle(ToggleUserLockCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var isCurrentlyLocked = await userManager.IsLockedOutAsync(user);

        if (isCurrentlyLocked)
        {
            // Unlock
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
            logger.LogInformation("SuperAdmin UNLOCKED user: {Email}", user.Email);
        }
        else
        {
            // Lock forever (or a long time)
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            logger.LogInformation("SuperAdmin LOCKED user: {Email}", user.Email);
        }

        var newIsLocked = !isCurrentlyLocked;
        return new ToggleUserLockResponse
        {
            IsLocked = newIsLocked,
            Message = newIsLocked ? "User account has been locked." : "User account has been unlocked."
        };
    }
}
