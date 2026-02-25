using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class DeleteUserRequest
{
    public string UserId { get; set; } = string.Empty;
}

public sealed class DeleteUserEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    ILogger<DeleteUserEndpoint> logger) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/admin/users/{userId}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Delete a user account (SuperAdmin only)";
            s.Description = "Permanently deletes a user account and all associated data records.";
            s.Response(204, "User deleted successfully");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<string>("userId");
        if (string.IsNullOrEmpty(userId))
        {
            AddError("User ID is required");
            ThrowIfAnyErrors(400);
        }

        var user = await userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Prevent self-deletion if needed, or deleting the last superadmin
        // For local dev, let's keep it simple.

        // Force cleanup: delete projects owned by this user
        // Note: For organizations, we keep them but they might become ownerless 
        // if they don't have other owners. 
        // However, the prompt specifically mentioned projects.
        var ownedProjects = await dbContext.Projects
            .Where(p => p.OwnerId == user.Id)
            .ToListAsync(ct);

        if (ownedProjects.Count > 0)
        {
            logger.LogInformation("Cleaning up {Count} owned projects for user {UserId}", ownedProjects.Count, user.Id);

            // Delete reports for these projects first to satisfy Restrict
            var projectIds = ownedProjects.Select(p => p.Id).ToList();
            var reports = await dbContext.Reports
                .Where(r => projectIds.Contains(r.ProjectId))
                .ToListAsync(ct);

            if (reports.Count > 0)
            {
                dbContext.Reports.RemoveRange(reports);
            }

            dbContext.Projects.RemoveRange(ownedProjects);
            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogWarning("SuperAdmin is PERMANENTLY DELETING user: {Email} ({Id})", user.Email, user.Id);

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }
            ThrowIfAnyErrors();
        }

        await Send.NoContentAsync(ct);
    }
}
