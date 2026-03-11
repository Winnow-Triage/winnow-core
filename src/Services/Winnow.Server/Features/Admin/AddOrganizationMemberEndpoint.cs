using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class AddOrganizationMemberRequest
{
    public Guid OrganizationId { get; set; }
    public string? UserId { get; set; } // If null, defaults to the current authenticated superadmin
    public string Role { get; set; } = "owner";
}

public class AddOrganizationMemberResponse
{
    public Guid MembershipId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Admin endpoint to manually add a user to an organization.
/// Primarily used by SuperAdmins to grant themselves or others access to a tenant.
/// </summary>
public sealed class AddOrganizationMemberEndpoint(
    WinnowDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILogger<AddOrganizationMemberEndpoint> logger) : Endpoint<AddOrganizationMemberRequest, AddOrganizationMemberResponse>
{
    public override void Configure()
    {
        Post("/admin/organizations/{organizationId}/members");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Add a member to an organization (SuperAdmin only)";
            s.Description = "Grants a user access to an organization with a specific role. If UserId is omitted, it defaults to the current superadmin.";
            s.Response<AddOrganizationMemberResponse>(200, "Member added successfully");
            s.Response(400, "Validation failure or user/org not found");
            s.Response(404, "Organization not found");
        });
    }

    public override async Task HandleAsync(AddOrganizationMemberRequest req, CancellationToken ct)
    {
        // 1. Verify Organization exists
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == req.OrganizationId, ct);

        if (org == null)
        {
            logger.LogWarning("Organization {OrgId} not found.", req.OrganizationId);
            await Send.NotFoundAsync(ct);
            return;
        }

        // 2. Resolve User
        string targetUserId = req.UserId ?? string.Empty;
        if (string.IsNullOrEmpty(targetUserId))
        {
            targetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        if (string.IsNullOrEmpty(targetUserId))
        {
            logger.LogError("Could not resolve user to add. ClaimTypes.NameIdentifier is missing.");
            ThrowError("Could not resolve user to add.");
        }

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user == null)
        {
            logger.LogError("User {UserId} not found in database. Session appears stale.", targetUserId);
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // 3. Check for existing membership (IgnoreQueryFilters because we want the real state)
        var existing = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == targetUserId && m.OrganizationId == req.OrganizationId, ct);

        if (existing)
        {
            await Send.OkAsync(new AddOrganizationMemberResponse
            {
                Message = "User is already a member of this organization."
            }, ct);
            return;
        }

        // 4. Create Membership
        var membership = new Domain.Organizations.OrganizationMember(
            req.OrganizationId,
            targetUserId,
            req.Role);

        dbContext.OrganizationMembers.Add(membership);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new AddOrganizationMemberResponse
        {
            MembershipId = membership.Id,
            Message = "Member added successfully."
        }, ct);
    }
}
