using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.List;

[RequirePermission("members:read")]
public record ListOrganizationMembersQuery(Guid CurrentOrganizationId) : IRequest<ListOrganizationMembersResult>, IOrgScopedRequest;

public record ListOrganizationMembersResult(bool IsSuccess, List<OrganizationDirectoryMemberDto>? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ListOrganizationMembersHandler(WinnowDbContext db) : IRequestHandler<ListOrganizationMembersQuery, ListOrganizationMembersResult>
{
    public async Task<ListOrganizationMembersResult> Handle(ListOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await db.OrganizationMembers
            .Where(om => om.OrganizationId == request.CurrentOrganizationId)
            .Join(db.Users, om => om.UserId, u => u.Id, (om, u) => new OrganizationDirectoryMemberDto
            {
                Id = Guid.Parse(om.UserId),
                FullName = u.FullName,
                Email = u.Email!,
                GlobalRole = om.Role.Name,
                RoleId = om.RoleId,
                Status = "Active",
                JoinedAt = om.JoinedAt,
                IsLocked = om.IsLocked
            })
            .ToListAsync(cancellationToken);

        var invitations = await db.OrganizationInvitations
            .Where(oi => oi.OrganizationId == request.CurrentOrganizationId)
            .Select(oi => new OrganizationDirectoryMemberDto
            {
                Id = oi.Id,
                FullName = null,
                Email = oi.Email.Value,
                GlobalRole = oi.Role.Name,
                RoleId = oi.RoleId,
                Status = "Pending",
                JoinedAt = oi.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = members
            .Concat(invitations)
            .OrderBy(x => x.Email)
            .ToList();

        return new ListOrganizationMembersResult(true, result);
    }
}
