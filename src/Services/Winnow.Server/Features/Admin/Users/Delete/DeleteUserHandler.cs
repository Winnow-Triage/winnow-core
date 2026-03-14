using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Users.Delete;

public record DeleteUserCommand : IRequest
{
    public string UserId { get; init; } = string.Empty;
}

public class DeleteUserHandler(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    ILogger<DeleteUserHandler> logger) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        // Prevent self-deletion if needed, or deleting the last superadmin
        // For local dev, let's keep it simple.

        // Force cleanup: delete projects owned by this user
        // Note: For organizations, we keep them but they might become ownerless 
        // if they don't have other owners. 
        // However, the prompt specifically mentioned projects.
        var ownedProjects = await dbContext.Projects
            .Where(p => p.OwnerId == user.Id)
            .ToListAsync(cancellationToken);

        if (ownedProjects.Count > 0)
        {
            logger.LogInformation("Cleaning up {Count} owned projects for user {UserId}", ownedProjects.Count, user.Id);

            // Delete reports for these projects first to satisfy Restrict
            var projectIds = ownedProjects.Select(p => p.Id).ToList();
            var reports = await dbContext.Reports
                .Where(r => projectIds.Contains(r.ProjectId))
                .ToListAsync(cancellationToken);

            if (reports.Count > 0)
            {
                dbContext.Reports.RemoveRange(reports);
            }

            dbContext.Projects.RemoveRange(ownedProjects);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogWarning("SuperAdmin is PERMANENTLY DELETING user: {Email} ({Id})", user.Email, user.Id);

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
