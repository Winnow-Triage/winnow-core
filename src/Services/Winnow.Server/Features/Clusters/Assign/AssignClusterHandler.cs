using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

using Winnow.Server.Infrastructure.Security.Authorization;

namespace Winnow.Server.Features.Clusters.Assign;

[RequirePermission("clusters:write")]
public record AssignClusterCommand(Guid OrgId, Guid Id, Guid ProjectId, string? AssignedTo) : IRequest<AssignClusterResult>, IOrgScopedRequest;

public record AssignClusterResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class AssignClusterHandler(WinnowDbContext db) : IRequestHandler<AssignClusterCommand, AssignClusterResult>
{
    public async Task<AssignClusterResult> Handle(AssignClusterCommand request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);

        if (cluster == null)
        {
            return new AssignClusterResult(false, "Cluster not found", 404);
        }

        cluster.AssignTo(request.AssignedTo);

        await db.SaveChangesAsync(cancellationToken);

        return new AssignClusterResult(true);
    }
}
