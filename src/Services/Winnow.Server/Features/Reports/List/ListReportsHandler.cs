using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Reports.List;

[RequirePermission("reports:read")]
public record ListReportsQuery(Guid OrgId, Guid ProjectId, string Sort) : IRequest<List<ReportDto>>, IOrgScopedRequest;

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
