using System;
using System.Threading;
using System.Threading.Tasks;

namespace Winnow.API.Features.Clusters.Search;

public record ClusterSearchFilters(
    string[]? Statuses = null,
    bool? IsOverage = null,
    bool? IsLocked = null,
    string SortBy = "relevanceScore",
    string SortOrder = "Desc"
);

public interface IClusterSearchRepository
{
    Task<PaginatedClusterSearchList<ClusterSearchDto>> GetRecentClustersAsync(Guid projectId, ClusterSearchFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedClusterSearchList<ClusterSearchDto>> HybridSearchClustersAsync(Guid projectId, string searchText, float[] searchVector, ClusterSearchFilters filters, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
