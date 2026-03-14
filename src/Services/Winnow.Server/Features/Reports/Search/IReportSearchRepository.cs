using System;
using System.Threading;
using System.Threading.Tasks;

namespace Winnow.Server.Features.Reports.Search;

public interface IReportSearchRepository
{
    Task<PaginatedSearchList<ReportSearchDto>> GetRecentlyUpdatedReportsAsync(Guid projectId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedSearchList<ReportSearchDto>> HybridSearchReportsAsync(Guid projectId, string searchText, float[] searchVector, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
