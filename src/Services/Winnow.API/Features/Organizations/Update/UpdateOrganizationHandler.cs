using Winnow.API.Features.Organizations.Get;
using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.Update;

[RequirePermission("settings:manage")]
public record UpdateOrganizationCommand(Guid CurrentOrganizationId, string Name) : IRequest<UpdateOrganizationResult>, IOrgScopedRequest;

public record UpdateOrganizationResult(bool IsSuccess, CurrentOrganizationResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class UpdateOrganizationHandler(WinnowDbContext db) : IRequestHandler<UpdateOrganizationCommand, UpdateOrganizationResult>
{
    public async Task<UpdateOrganizationResult> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

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
