using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Services;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Services.Ai;

/// <summary>
/// Implementation of IClusterService.
/// </summary>
public class ClusterService(
    WinnowDbContext dbContext,
    IVectorCalculator vectorCalculator,
    ILogger<ClusterService> logger) : IClusterService
{
    /// <inheritdoc />
    public async Task RecalculateCentroidAsync(Guid clusterId, CancellationToken ct = default)
    {
        var cluster = await dbContext.Clusters.FindAsync([clusterId], ct);
        if (cluster == null)
        {
            logger.LogWarning("ClusterService: Cluster {Id} not found for centroid recalculation.", clusterId);
            return;
        }

        var memberEmbeddings = await dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ClusterId == cluster.Id && r.Embedding != null)
            .Select(r => r.Embedding!)
            .ToListAsync(ct);

        if (memberEmbeddings.Count > 0)
        {
            cluster.UpdateCentroid(vectorCalculator.CalculateCentroid(memberEmbeddings));
            logger.LogInformation("ClusterService: Recalculated centroid for cluster {Id} ({Count} reports).", clusterId, memberEmbeddings.Count);

            // Fetch reports again to update confidence scores (we need tracked entities)
            var membersToUpdate = await dbContext.Reports
                .Where(r => r.ClusterId == cluster.Id && r.Embedding != null)
                .ToListAsync(ct);

            foreach (var member in membersToUpdate)
            {
                var distance = vectorCalculator.CalculateCosineDistance(member.Embedding!, cluster.Centroid!);
                // Similarity = 1 - Distance. Convert to percentage.
                member.SetConfidenceScore(new ConfidenceScore(Math.Clamp(1.0 - distance, 0.0, 1.0)));
            }

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("ClusterService: Updated confidence scores for {Count} reports in cluster {Id}.", membersToUpdate.Count, clusterId);
        }
        else
        {
            logger.LogDebug("ClusterService: No member reports found for cluster {Id}.", clusterId);
        }
    }
}
