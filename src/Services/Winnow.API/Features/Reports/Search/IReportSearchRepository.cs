using System;
using System.Threading;
using System.Threading.Tasks;

namespace Winnow.API.Features.Reports.Search;

public record ReportSearchFilters(
    string[]? Statuses = null,
    Guid? ClusterId = null,
    bool? IsOverage = null,
    bool? IsLocked = null,
    string? AssignedTo = null,
    string SortBy = "UpdatedAt",
    string SortOrder = "Desc"
);

public interface IReportSearchRepository
{
    Task<PaginatedSearchList<ReportSearchDto>> GetRecentlyUpdatedReportsAsync(
        Guid projectId,
        int pageNumber,
        int pageSize,
        ReportSearchFilters filters,
        CancellationToken cancellationToken = default);

    Task<PaginatedSearchList<ReportSearchDto>> HybridSearchReportsAsync(
        Guid projectId,
        string searchText,
        float[] searchVector,
        int pageNumber,
        int pageSize,
        ReportSearchFilters filters,
        CancellationToken cancellationToken = default);
}
