using System;
using System.Threading;
using System.Threading.Tasks;

namespace Winnow.API.Features.Clusters.Search;

public interface IClusterSearchRepository
{
    Task<PaginatedClusterSearchList<ClusterSearchDto>> GetRecentClustersAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedClusterSearchList<ClusterSearchDto>> HybridSearchClustersAsync(Guid projectId, string searchText, float[] searchVector, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
