using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

public class OrganizationDirectoryMemberDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string GlobalRole { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? JoinedAt { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class ListOrganizationMembersEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : EndpointWithoutRequest<List<OrganizationDirectoryMemberDto>>
{
    public override void Configure()
    {
        Get("/organizations/current/members");
        Summary(s =>
        {
            s.Summary = "List all members of the current organization";
            s.Description = "Returns a unified list of active members and pending invitations for the active organization.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        var orgId = tenantContext.CurrentOrganizationId.Value;

        // 1. Query Active Members
        var members = await db.OrganizationMembers
            .Where(om => om.OrganizationId == orgId)
            .Include(om => om.User)
            .Select(om => new OrganizationDirectoryMemberDto
            {
                // Note: Identity User Id is generally a string. 
                // We'll try to parse it to Guid as requested by the DTO spec.
                Id = Guid.Parse(om.UserId),
                FullName = om.User!.FullName,
                Email = om.User!.Email!,
                GlobalRole = om.Role,
                Status = "Active",
                JoinedAt = om.JoinedAt,
                IsLocked = om.IsLocked
            })
            .ToListAsync(ct);

        // 2. Query Pending Invitations
        var invitations = await db.OrganizationInvitations
            .Where(oi => oi.OrganizationId == orgId)
            .Select(oi => new OrganizationDirectoryMemberDto
            {
                Id = oi.Id,
                FullName = null,
                Email = oi.Email,
                GlobalRole = oi.Role,
                Status = "Pending",
                JoinedAt = oi.CreatedAt
            })
            .ToListAsync(ct);

        // 3. Combine and Sort
        var result = members
            .Concat(invitations)
            .OrderBy(x => x.Email)
            .ToList();

        await Send.OkAsync(result, ct);
    }
}
