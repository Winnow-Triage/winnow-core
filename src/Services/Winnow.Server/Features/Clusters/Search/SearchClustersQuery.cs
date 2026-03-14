using MediatR;
using System;
using System.Collections.Generic;

namespace Winnow.Server.Features.Clusters.Search;

public class ClusterSearchDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CriticalityScore { get; set; }
    public int ReportCount { get; set; }
    public bool IsLocked { get; set; }
    public bool IsOverage { get; set; }
    public double? RelevanceScore { get; set; }
}

public class PaginatedClusterSearchList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public PaginatedClusterSearchList(IReadOnlyCollection<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public record SearchClustersQuery(Guid ProjectId, string SearchTerm, int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedClusterSearchList<ClusterSearchDto>>;
