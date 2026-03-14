using Winnow.Server.Features.Organizations.Get;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Update;

public record UpdateOrganizationCommand(Guid OrganizationId, string Name) : IRequest<UpdateOrganizationResult>;

public record UpdateOrganizationResult(bool IsSuccess, CurrentOrganizationResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class UpdateOrganizationHandler(WinnowDbContext db) : IRequestHandler<UpdateOrganizationCommand, UpdateOrganizationResult>
{
    public async Task<UpdateOrganizationResult> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization == null)
        {
            return new UpdateOrganizationResult(false, null, "Organization not found", 404);
        }

        organization.Rename(request.Name);

        await db.SaveChangesAsync(cancellationToken);

        var data = new CurrentOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.Plan.Name ?? "Free",
            CreatedAt = organization.CreatedAt
        };

        return new UpdateOrganizationResult(true, data);
    }
}
