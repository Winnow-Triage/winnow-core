using MediatR;
using System;
using System.Collections.Generic;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.Search;

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

[RequirePermission("reports:read")]
public record SearchReportsQuery(
    Guid CurrentOrganizationId,
    Guid ProjectId,
    string SearchTerm,
    int PageNumber = 1,
    int PageSize = 20,
    string[]? Statuses = null,
    Guid? ClusterId = null,
    bool? IsOverage = null,
    bool? IsLocked = null,
    string? AssignedTo = null,
    string SortBy = "UpdatedAt",
    string SortOrder = "Desc"
) : IRequest<PaginatedSearchList<ReportSearchDto>>, IOrgScopedRequest;
