using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Winnow.API.Services.Ai;

namespace Winnow.API.Features.Clusters.Search;

public class SearchClustersHandler : IRequestHandler<SearchClustersQuery, PaginatedClusterSearchList<ClusterSearchDto>>
{
    private readonly IClusterSearchRepository _clusterSearchRepository;
    private readonly IEmbeddingService _embeddingService;

    public SearchClustersHandler(IClusterSearchRepository clusterSearchRepository, IEmbeddingService embeddingService)
    {
        _clusterSearchRepository = clusterSearchRepository;
        _embeddingService = embeddingService;
    }

    public async Task<PaginatedClusterSearchList<ClusterSearchDto>> Handle(SearchClustersQuery request, CancellationToken cancellationToken)
    {
        var filters = new ClusterSearchFilters(
            request.Statuses,
            request.IsOverage,
            request.IsLocked,
            request.SortBy,
            request.SortOrder
        );

        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return await _clusterSearchRepository.GetRecentClustersAsync(
                request.ProjectId,
                filters,
                request.PageNumber,
                request.PageSize,
                cancellationToken);
        }

        float[] searchVector = (await _embeddingService.GetEmbeddingAsync(request.SearchTerm)).Vector;

        return await _clusterSearchRepository.HybridSearchClustersAsync(
            request.ProjectId,
            request.SearchTerm,
            searchVector,
            filters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
