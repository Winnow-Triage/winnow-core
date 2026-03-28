using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Auth.Get;

public record GetMeQuery(string UserId, string? OrgIdClaim) : IRequest<GetMeResult>;

public record GetMeResult(
    bool IsSuccess,
    string Id,
    string Email,
    string FullName,
    bool IsEmailVerified,
    List<string> Roles,
    List<string> Permissions,
    Guid? ActiveOrganizationId,
    Guid? DefaultProjectId);

public class GetMeHandler(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext) : IRequestHandler<GetMeQuery, GetMeResult>
{
    public async Task<GetMeResult> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return new GetMeResult(false, "", "", "", false, [], [], null, null);
        }

        Guid? activeOrgId = null;
        if (Guid.TryParse(request.OrgIdClaim, out var parsedOrgId))
        {
            activeOrgId = parsedOrgId;
        }

        Guid? defaultProjectId = null;
        if (activeOrgId.HasValue)
        {
            var project = await dbContext.Projects
                .Where(p => p.OrganizationId == activeOrgId.Value)
                .OrderBy(p => p.CreatedAt)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (project != Guid.Empty)
            {
                defaultProjectId = project;
            }
        }

        List<string> permissions = [];
        if (activeOrgId.HasValue)
        {
            permissions = await dbContext.OrganizationMembers
                .Where(om => om.UserId == user.Id && om.OrganizationId == activeOrgId.Value && !om.IsLocked)
                .SelectMany(om => om.Role!.Permissions)
                .Select(rp => rp.Permission!.Name)
                .ToListAsync(cancellationToken);
        }

        var roles = await userManager.GetRolesAsync(user);

        return new GetMeResult(
            true,
            user.Id,
            user.Email ?? "",
            user.FullName,
            user.EmailConfirmed,
            roles.ToList(),
            permissions,
            activeOrgId,
            defaultProjectId
        );
    }
}
