using MediatR;
using System;
using System.Collections.Generic;

namespace Winnow.Server.Features.Reports.Search;

public class ReportSearchDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public Guid? ClusterId { get; set; }
    public bool IsOverage { get; set; }
    public bool IsLocked { get; set; }
    public double? RelevanceScore { get; set; }
}

public class PaginatedSearchList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public PaginatedSearchList(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public record SearchReportsQuery(Guid ProjectId, string SearchTerm, int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedSearchList<ReportSearchDto>>;
