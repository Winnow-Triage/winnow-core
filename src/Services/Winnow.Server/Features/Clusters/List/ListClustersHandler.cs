using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Clusters.List;

[RequirePermission("clusters:read")]
public record ListClustersQuery(Guid CurrentOrganizationId, Guid ProjectId, string Sort) : IRequest<ListClustersResult>, IOrgScopedRequest;

public record ListClustersResult(bool IsSuccess, List<ClusterDto>? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class ListClustersHandler(WinnowDbContext dbContext) : IRequestHandler<ListClustersQuery, ListClustersResult>
{
    public async Task<ListClustersResult> Handle(ListClustersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Clusters
            .AsNoTracking()
            .Where(c => c.ProjectId == request.ProjectId);

        var clusters = await query
            .Select(c => new ClusterDto(
                c.Id,
                c.Title,
                c.Summary,
                c.CriticalityScore,
                c.Status.Name,
                c.CreatedAt,
                dbContext.Reports.Count(r => r.ClusterId == c.Id),
                dbContext.Reports.Any(r => r.ClusterId == c.Id && r.IsLocked),
                dbContext.Reports.Any(r => r.ClusterId == c.Id && r.IsOverage)))
            .ToListAsync(cancellationToken);

        var sortedClusters = request.Sort switch
        {
            "criticality" => clusters.OrderByDescending(c => c.CriticalityScore ?? 0).ThenByDescending(c => c.ReportCount),
            "newest" => clusters.OrderByDescending(c => c.CreatedAt),
            _ => clusters.OrderByDescending(c => c.ReportCount)
        };

        return new ListClustersResult(true, sortedClusters.ToList());
    }
}
