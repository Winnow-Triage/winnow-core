using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations;

public class OrganizationMemberResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class ListOrganizationMembersEndpoint(WinnowDbContext db, ITenantContext tenantContext)
    : EndpointWithoutRequest<List<OrganizationMemberResponse>>
{
    public override void Configure()
    {
        Get("/organizations/current/members");
        Summary(s =>
        {
            s.Summary = "List all members of the current organization";
            s.Description = "Returns a list of all users who are members of the active organization.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!tenantContext.CurrentOrganizationId.HasValue)
        {
            ThrowError("No organization context.");
        }

        var members = await db.OrganizationMembers
            .Where(om => om.OrganizationId == tenantContext.CurrentOrganizationId.Value)
            .Include(om => om.User)
            .Select(om => new OrganizationMemberResponse
            {
                UserId = om.UserId,
                FullName = om.User!.FullName,
                Email = om.User!.Email!,
                Role = om.Role
            })
            .ToListAsync(ct);

        await Send.OkAsync(members, ct);
    }
}
