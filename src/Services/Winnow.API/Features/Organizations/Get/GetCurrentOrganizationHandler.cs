using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Organizations.Get;

[RequirePermission("organizations:read")]
public record GetCurrentOrganizationQuery(Guid CurrentOrganizationId) : IRequest<GetCurrentOrganizationResult>, IOrgScopedRequest;

public record GetCurrentOrganizationResult(bool IsSuccess, CurrentOrganizationResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetCurrentOrganizationHandler(WinnowDbContext db) : IRequestHandler<GetCurrentOrganizationQuery, GetCurrentOrganizationResult>
{
    public async Task<GetCurrentOrganizationResult> Handle(GetCurrentOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.CurrentOrganizationId, cancellationToken);

        if (organization == null)
        {
            return new GetCurrentOrganizationResult(false, null, "Organization not found", 404);
        }

        var data = new CurrentOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.Plan.Name ?? "Free",
            CreatedAt = organization.CreatedAt
        };

        return new GetCurrentOrganizationResult(true, data);
    }
}
