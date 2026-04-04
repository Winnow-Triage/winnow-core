using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Clusters.Merge;

[RequirePermission("clusters:write")]
public record DismissClusterMergeSuggestionCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<DismissClusterMergeSuggestionResult>, IOrgScopedRequest;

public record DismissClusterMergeSuggestionResult(bool IsSuccess, string? ErrorMessage = null, int? StatusCode = null);

public class DismissClusterMergeSuggestionHandler(WinnowDbContext db, INegativeMatchCache negativeMatchCache) : IRequestHandler<DismissClusterMergeSuggestionCommand, DismissClusterMergeSuggestionResult>
{
    public async Task<DismissClusterMergeSuggestionResult> Handle(DismissClusterMergeSuggestionCommand request, CancellationToken cancellationToken)
    {
        var cluster = await db.Clusters
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.ProjectId == request.ProjectId, cancellationToken);

        if (cluster == null)
        {
            return new DismissClusterMergeSuggestionResult(false, "Cluster not found", 404);
        }

        if (cluster.SuggestedMergeClusterId == null)
        {
            return new DismissClusterMergeSuggestionResult(false, "No pending merge suggestion for this cluster.", 400);
        }

        await negativeMatchCache.MarkAsMismatchAsync(request.CurrentOrganizationId.ToString(), cluster.Id, cluster.SuggestedMergeClusterId.Value);

        cluster.ClearMergeSuggestion();

        await db.SaveChangesAsync(cancellationToken);

        return new DismissClusterMergeSuggestionResult(true);
    }
}
