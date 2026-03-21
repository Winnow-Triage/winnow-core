using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Admin.Organizations.RemoveMember;

public record RemoveOrganizationMemberCommand : IRequest
{
    public Guid OrganizationId { get; init; }
    public string UserId { get; init; } = string.Empty;
}

public class RemoveOrganizationMemberHandler(
    WinnowDbContext dbContext,
    ILogger<RemoveOrganizationMemberHandler> logger) : IRequestHandler<RemoveOrganizationMemberCommand>
{
    public async Task Handle(RemoveOrganizationMemberCommand request, CancellationToken cancellationToken)
    {
        var membership = await dbContext.OrganizationMembers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.OrganizationId == request.OrganizationId && m.UserId == request.UserId, cancellationToken);

        if (membership == null)
        {
            throw new InvalidOperationException("Membership not found.");
        }

        logger.LogWarning("SuperAdmin is REMOVING user {UserId} from organization {OrgId}", request.UserId, request.OrganizationId);

        dbContext.OrganizationMembers.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
