using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Shared;

public abstract class ProjectScopedEndpoint<TRequest, TResponse> : OrganizationScopedEndpoint<TRequest, TResponse>
    where TRequest : ProjectScopedRequest, new()
    where TResponse : notnull
{
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken cancellationToken)
    {
        // This populates req.OrganizationId and ensures the tenant context is valid.
        await base.OnBeforeHandleAsync(req, cancellationToken);

        // Admins and Owners can see everything in the Org
        if (req.HasAnyRole("Admin", "SuperAdmin", "Owner")) return;

        var db = Resolve<WinnowDbContext>();

        // Prioritize the project ID from the route if it's provided (e.g. for management operations)
        var targetProjectId = req.ProjectId != Guid.Empty ? req.ProjectId : req.CurrentProjectId;

        if (targetProjectId == Guid.Empty)
        {
            ThrowError("Project ID is required.", 400);
        }

        // Check if user is the owner, a direct member, or a member of the assigned team
        var hasAccess = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == targetProjectId && p.OrganizationId == req.CurrentOrganizationId)
            .AnyAsync(p =>
                p.OwnerId == req.CurrentUserId ||
                db.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == req.CurrentUserId) ||
                (p.TeamId != null && db.TeamMembers.Any(tm => tm.TeamId == p.TeamId && tm.UserId == req.CurrentUserId)),
                cancellationToken);

        if (!hasAccess)
        {
            ThrowError("Project not found or access denied", 404);
        }
    }
}

public abstract class ProjectScopedEndpoint<TRequest> : OrganizationScopedEndpoint<TRequest>
    where TRequest : ProjectScopedRequest, new()
{
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken cancellationToken)
    {
        await base.OnBeforeHandleAsync(req, cancellationToken);

        // Admins and Owners can see everything in the Org
        if (req.HasAnyRole("Admin", "SuperAdmin", "Owner")) return;

        var db = Resolve<WinnowDbContext>();

        // Prioritize the project ID from the route
        var targetProjectId = req.ProjectId != Guid.Empty ? req.ProjectId : req.CurrentProjectId;

        if (targetProjectId == Guid.Empty)
        {
            ThrowError("Project ID is required.", 400);
        }

        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == targetProjectId &&
                p.OrganizationId == req.CurrentOrganizationId &&
                p.OwnerId == req.CurrentUserId, cancellationToken);

        if (!userOwnsProject) ThrowError("Project not found or access denied", 404);
    }
}