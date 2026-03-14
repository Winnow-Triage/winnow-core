using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.List;

public record ListOrganizationMembersQuery(Guid OrganizationId) : IRequest<ListOrganizationMembersResult>;

public record ListOrganizationMembersResult(bool IsSuccess, List<OrganizationDirectoryMemberDto>? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ListOrganizationMembersHandler(WinnowDbContext db) : IRequestHandler<ListOrganizationMembersQuery, ListOrganizationMembersResult>
{
    public async Task<ListOrganizationMembersResult> Handle(ListOrganizationMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await db.OrganizationMembers
            .Where(om => om.OrganizationId == request.OrganizationId)
            .Join(db.Users, om => om.UserId, u => u.Id, (om, u) => new OrganizationDirectoryMemberDto
            {
                Id = Guid.Parse(om.UserId),
                FullName = u.FullName,
                Email = u.Email!,
                GlobalRole = om.Role,
                Status = "Active",
                JoinedAt = om.JoinedAt,
                IsLocked = om.IsLocked
            })
            .ToListAsync(cancellationToken);

        var invitations = await db.OrganizationInvitations
            .Where(oi => oi.OrganizationId == request.OrganizationId)
            .Select(oi => new OrganizationDirectoryMemberDto
            {
                Id = oi.Id,
                FullName = null,
                Email = oi.Email.Value,
                GlobalRole = oi.Role,
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
