using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Admin.Organizations.List;

public record ListAllOrganizationsQuery : IRequest<List<OrganizationSummaryResponse>>;

public class ListAllOrganizationsHandler(WinnowDbContext dbContext) : IRequestHandler<ListAllOrganizationsQuery, List<OrganizationSummaryResponse>>
{
    public async Task<List<OrganizationSummaryResponse>> Handle(ListAllOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var result = await dbContext.Organizations
            .IgnoreQueryFilters()
            .Select(o => new OrganizationSummaryResponse
            {
                Id = o.Id,
                Name = o.Name,
                StripeCustomerId = o.BillingIdentity.HasValue ? o.BillingIdentity.Value.CustomerId : null,
                SubscriptionTier = o.Plan.Name,
                CreatedAt = o.CreatedAt,
                IsSuspended = o.IsSuspended,
                TeamCount = dbContext.Teams.IgnoreQueryFilters().Count(t => t.OrganizationId == o.Id),
                MemberCount = dbContext.OrganizationMembers.IgnoreQueryFilters().Count(m => m.OrganizationId == o.Id),
                ProjectCount = dbContext.Projects.IgnoreQueryFilters().Count(p => p.OrganizationId == o.Id)
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}
