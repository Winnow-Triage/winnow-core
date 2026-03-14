using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Users.List;

public record ListAllUsersQuery : IRequest<List<UserSummaryResponse>>;

public class ListAllUsersHandler(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext) : IRequestHandler<ListAllUsersQuery, List<UserSummaryResponse>>
{
    public async Task<List<UserSummaryResponse>> Handle(ListAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .Include(u => u.OrganizationMemberships)
            .ToListAsync(cancellationToken);

        // Get all organizations involved
        var orgIds = users.SelectMany(u => u.OrganizationMemberships).Select(om => om.OrganizationId).Distinct().ToList();
        var orgNames = await dbContext.Organizations
            .Where(o => orgIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var responses = new List<UserSummaryResponse>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLocked = await userManager.IsLockedOutAsync(user);

            responses.Add(new UserSummaryResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt,
                IsLockedOut = isLocked,
                Organizations = user.OrganizationMemberships.Select(om => new UserOrganization
                {
                    Id = om.OrganizationId,
                    Name = orgNames.GetValueOrDefault(om.OrganizationId, "Unknown")
                }).ToList()
            });
        }

        return responses;
    }
}
