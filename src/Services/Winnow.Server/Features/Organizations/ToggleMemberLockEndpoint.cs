using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

public sealed class ToggleMemberLockEndpoint(WinnowDbContext db)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Put("/organizations/{orgId}/members/{userId}/lock");
        Summary(s =>
        {
            s.Summary = "Toggle member lock status";
            s.Description = "Locks or restores access for a member in the organization.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orgIdRaw = Route<string>("orgId");
        var memberUserId = Route<string>("userId");
        Console.WriteLine($"[LOCK] Attempting to toggle lock for user {memberUserId} in organization {orgIdRaw}");

        Guid orgId = Guid.Empty;
        if (!Guid.TryParse(orgIdRaw, out orgId))
        {
            Console.WriteLine($"[LOCK] INVALID ORGID: {orgIdRaw}");
            AddError("Invalid organization context");
            ThrowIfAnyErrors();
        }

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var isOwner = await db.OrganizationMembers
            .AnyAsync(om => om.OrganizationId == orgId && om.UserId == currentUserId && (om.Role == "owner" || om.Role == "Admin"), ct);

        if (!isOwner)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(om => om.OrganizationId == orgId && om.UserId == memberUserId, ct);

        if (member == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        member.IsLocked = !member.IsLocked;
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new { member.IsLocked }, ct);
    }
}
