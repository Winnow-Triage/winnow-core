using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Winnow.API.Services.Ai; // Using the existing IEmbeddingService

namespace Winnow.API.Features.Reports.Search;

public class SearchReportsHandler : IRequestHandler<SearchReportsQuery, PaginatedSearchList<ReportSearchDto>>
{
    private readonly IReportSearchRepository _reportSearchRepository;
    private readonly IEmbeddingService _embeddingService;

    public SearchReportsHandler(IReportSearchRepository reportSearchRepository, IEmbeddingService embeddingService)
    {
        _reportSearchRepository = reportSearchRepository;
        _embeddingService = embeddingService;
    }

    public async Task<PaginatedSearchList<ReportSearchDto>> Handle(SearchReportsQuery request, CancellationToken cancellationToken)
    {
        var filters = new ReportSearchFilters(
            request.Statuses,
            request.ClusterId,
            request.IsOverage,
            request.IsLocked,
            request.AssignedTo,
            request.SortBy,
            request.SortOrder
        );

        // 1. Fast Path Guard Clause: If search string is empty or null, bypass search logic
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return await _reportSearchRepository.GetRecentlyUpdatedReportsAsync(
                request.ProjectId,
                request.PageNumber,
                request.PageSize,
                filters,
                cancellationToken);
        }

        // 2. Vector Generation: Convert the search query into a vector representation
        float[] searchVector = (await _embeddingService.GetEmbeddingAsync(request.SearchTerm)).Vector;

        // 3 & 4. Hybrid Database Query with PostgreSQL returning RRF sorted results
        return await _reportSearchRepository.HybridSearchReportsAsync(
            request.ProjectId,
            request.SearchTerm,
            searchVector,
            request.PageNumber,
            request.PageSize,
            filters,
            cancellationToken);
    }
}
