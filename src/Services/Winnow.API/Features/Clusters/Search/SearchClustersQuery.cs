using MediatR;
using System;
using System.Collections.Generic;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Clusters.Search;

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
    public bool IsSummarizing { get; set; }
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

[RequirePermission("clusters:read")]
public record SearchClustersQuery(
    Guid CurrentOrganizationId,
    Guid ProjectId,
    string SearchTerm,
    string[]? Statuses = null,
    bool? IsOverage = null,
    bool? IsLocked = null,
    string SortBy = "relevanceScore",
    string SortOrder = "Desc",
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedClusterSearchList<ClusterSearchDto>>, IOrgScopedRequest;
