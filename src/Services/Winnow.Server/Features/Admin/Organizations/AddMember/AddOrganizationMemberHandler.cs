using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Organizations.AddMember;

public record AddOrganizationMemberCommand : IRequest<AddOrganizationMemberResponse>
{
    public Guid OrganizationId { get; init; }
    public string TargetUserId { get; init; } = string.Empty;
    public string Role { get; init; } = "owner";
}

public class AddOrganizationMemberHandler(
    WinnowDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILogger<AddOrganizationMemberHandler> logger) : IRequestHandler<AddOrganizationMemberCommand, AddOrganizationMemberResponse>
{
    public async Task<AddOrganizationMemberResponse> Handle(AddOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Organization exists
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (org == null)
        {
            logger.LogWarning("Organization {OrgId} not found.", request.OrganizationId);
            throw new InvalidOperationException("Organization not found.");
        }

        // 2. Resolve User
        if (string.IsNullOrEmpty(request.TargetUserId))
        {
            logger.LogError("Could not resolve user to add. TargetUserId is missing.");
            throw new ArgumentException("Could not resolve user to add.");
        }

        var user = await userManager.FindByIdAsync(request.TargetUserId);
        if (user == null)
        {
            logger.LogError("User {UserId} not found in database.", request.TargetUserId);
            throw new UnauthorizedAccessException("User not found.");
        }

        // 3. Check for existing membership (IgnoreQueryFilters because we want the real state)
        var existing = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == request.TargetUserId && m.OrganizationId == request.OrganizationId, cancellationToken);

        if (existing)
        {
            return new AddOrganizationMemberResponse
            {
                Message = "User is already a member of this organization."
            };
        }

        // 4. Create Membership
        var membership = new OrganizationMember(
            request.OrganizationId,
            request.TargetUserId,
            request.Role);

        dbContext.OrganizationMembers.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddOrganizationMemberResponse
        {
            MembershipId = membership.Id,
            Message = "Member added successfully."
        };
    }
}
