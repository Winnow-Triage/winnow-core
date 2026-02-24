using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class RemoveOrganizationMemberRequest
{
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class RemoveOrganizationMemberEndpoint(
    WinnowDbContext dbContext,
    ILogger<RemoveOrganizationMemberEndpoint> logger) : Endpoint<RemoveOrganizationMemberRequest>
{
    public override void Configure()
    {
        Delete("/admin/organizations/{organizationId}/members/{userId}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Remove a user from an organization (SuperAdmin only)";
            s.Description = "Removes the specified user's membership from an organization. This is a powerful administrative action.";
            s.Response(204, "Member removed successfully");
            s.Response(404, "Membership not found");
        });
    }

    public override async Task HandleAsync(RemoveOrganizationMemberRequest req, CancellationToken ct)
    {
        var membership = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.OrganizationId == req.OrganizationId && m.UserId == req.UserId, ct);

        if (membership == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        logger.LogWarning("SuperAdmin is REMOVING user {UserId} from organization {OrgId}", req.UserId, req.OrganizationId);

        dbContext.OrganizationMembers.Remove(membership);
        await dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
