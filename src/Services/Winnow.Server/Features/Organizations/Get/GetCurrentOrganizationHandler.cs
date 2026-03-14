using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Get;

public record GetCurrentOrganizationQuery(Guid OrganizationId) : IRequest<GetCurrentOrganizationResult>;

public record GetCurrentOrganizationResult(bool IsSuccess, CurrentOrganizationResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetCurrentOrganizationHandler(WinnowDbContext db) : IRequestHandler<GetCurrentOrganizationQuery, GetCurrentOrganizationResult>
{
    public async Task<GetCurrentOrganizationResult> Handle(GetCurrentOrganizationQuery request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

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
