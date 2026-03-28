using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.List;

[RequirePermission("reports:read")]
public record ListReportsQuery(Guid CurrentOrganizationId, Guid ProjectId, string Sort) : IRequest<List<ReportDto>>, IOrgScopedRequest;

public class ListReportsHandler(WinnowDbContext dbContext) : IRequestHandler<ListReportsQuery, List<ReportDto>>
{
    public async Task<List<ReportDto>> Handle(ListReportsQuery request, CancellationToken cancellationToken)
    {
        var sort = request.Sort;
        if (string.IsNullOrEmpty(sort)) sort = "newest";

        var query = dbContext.Reports.AsNoTracking()
            .Where(r => r.ProjectId == request.ProjectId);

        query = sort switch
        {
            "confidence" => query.OrderByDescending(r => r.ConfidenceScore).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var reports = await query
            .Select(r => new ReportDto(
                r.Id,
                r.Title,
                r.Message,
                r.StackTrace,
                r.Status.Name,
                r.CreatedAt,
                r.ClusterId,
                r.ConfidenceScore!.Value.Score,
                r.Metadata,
                r.IsOverage,
                r.IsLocked))
            .ToListAsync(cancellationToken);

        return reports;
    }
}
