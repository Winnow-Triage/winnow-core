using Winnow.Server.Features.Organizations.Get;
using MediatR;
using Winnow.Server.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Organizations.Update;

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
