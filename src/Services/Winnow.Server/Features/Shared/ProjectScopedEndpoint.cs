using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Shared;

public abstract class ProjectScopedEndpoint<TRequest, TResponse> : OrganizationScopedEndpoint<TRequest, TResponse>
    where TRequest : ProjectScopedRequest, new()
    where TResponse : notnull
{
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken ct)
    {
        // This populates req.OrganizationId and ensures the tenant context is valid.
        await base.OnBeforeHandleAsync(req, ct);

        // Admins and Owners can see everything in the Org
        if (req.HasAnyRole("Admin", "SuperAdmin", "Owner")) return;

        var db = Resolve<WinnowDbContext>();

        // Check if user is the owner, a direct member, or a member of the assigned team
        var hasAccess = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == req.CurrentProjectId && p.OrganizationId == req.CurrentOrganizationId)
            .AnyAsync(p =>
                p.OwnerId == req.CurrentUserId ||
                db.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == req.CurrentUserId) ||
                (p.TeamId != null && db.TeamMembers.Any(tm => tm.TeamId == p.TeamId && tm.UserId == req.CurrentUserId)),
                ct);

        if (!hasAccess)
        {
            ThrowError("Project not found or access denied", 404);
        }
    }
}

public abstract class ProjectScopedEndpoint<TRequest> : OrganizationScopedEndpoint<TRequest>
    where TRequest : ProjectScopedRequest, new()
{
    public override async Task OnBeforeHandleAsync(TRequest req, CancellationToken ct)
    {
        await base.OnBeforeHandleAsync(req, ct);

        // Admins and Owners can see everything in the Org
        if (req.HasAnyRole("Admin", "SuperAdmin", "Owner")) return;

        var db = Resolve<WinnowDbContext>();
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id == req.CurrentProjectId &&
                p.OrganizationId == req.CurrentOrganizationId &&
                p.OwnerId == req.CurrentUserId, ct);

        if (!userOwnsProject) ThrowError("Project not found or access denied", 404);
    }
}