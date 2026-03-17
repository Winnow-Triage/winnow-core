using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Clusters.GenerateSummary;

[RequirePermission("clusters:write")]
public record ClearClusterSummaryCommand(Guid OrgId, Guid Id, Guid ProjectId) : IRequest<ClearClusterSummaryResult>, IOrgScopedRequest;

public record ClearClusterSummaryResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class ClearClusterSummaryHandler(WinnowDbContext db) : IRequestHandler<ClearClusterSummaryCommand, ClearClusterSummaryResult>
{
    public async Task<ClearClusterSummaryResult> Handle(ClearClusterSummaryCommand request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);

        if (cluster == null)
        {
            return new ClearClusterSummaryResult(false, "Cluster not found", 404);
        }

        cluster.ClearSummary();

        await db.SaveChangesAsync(cancellationToken);

        return new ClearClusterSummaryResult(true);
    }
}
