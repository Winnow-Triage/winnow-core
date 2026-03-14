using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.List;

public record ListUserOrganizationsQuery(string UserId) : IRequest<ListUserOrganizationsResult>;

public record ListUserOrganizationsResult(bool IsSuccess, List<OrganizationDto>? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ListUserOrganizationsHandler(WinnowDbContext db) : IRequestHandler<ListUserOrganizationsQuery, ListUserOrganizationsResult>
{
    public async Task<ListUserOrganizationsResult> Handle(ListUserOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var organizations = await db.OrganizationMembers
            .Where(om => om.UserId == request.UserId)
            .Join(db.Organizations, om => om.OrganizationId, o => o.Id, (om, o) => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name
            })
            .ToListAsync(cancellationToken);

        return new ListUserOrganizationsResult(true, organizations);
    }
}
