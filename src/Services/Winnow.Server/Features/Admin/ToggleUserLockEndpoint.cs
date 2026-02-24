using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Entities;

namespace Winnow.Server.Features.Admin;

public class ToggleUserLockRequest
{
    public string Id { get; set; } = string.Empty;
}

public class ToggleUserLockResponse
{
    public bool IsLocked { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ToggleUserLockEndpoint(
    UserManager<ApplicationUser> userManager,
    ILogger<ToggleUserLockEndpoint> logger) : Endpoint<ToggleUserLockRequest, ToggleUserLockResponse>
{
    public override void Configure()
    {
        Post("/admin/users/{id}/toggle-lock");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Lock or unlock a user account (SuperAdmin only)";
            s.Description = "Toggles the account lockout status for a user. If locked, the user cannot log in.";
            s.Response<ToggleUserLockResponse>(200, "Status toggled successfully");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(ToggleUserLockRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id);
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
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
        await Send.OkAsync(new ToggleUserLockResponse
        {
            IsLocked = newIsLocked,
            Message = newIsLocked ? "User account has been locked." : "User account has been unlocked."
        }, ct);
    }
}
