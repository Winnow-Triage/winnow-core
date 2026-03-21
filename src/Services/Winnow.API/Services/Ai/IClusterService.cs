using System;
using System.Threading;
using System.Threading.Tasks;

namespace Winnow.API.Services.Ai;

/// <summary>
/// Service for cluster-related operations.
/// </summary>
public interface IClusterService
{
    /// <summary>
    /// Recalculates the centroid for a given cluster based on its member reports' embeddings.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecalculateCentroidAsync(Guid clusterId, CancellationToken ct = default);
}
