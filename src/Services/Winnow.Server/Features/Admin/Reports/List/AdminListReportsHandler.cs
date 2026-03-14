using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Reports.List;

public record AdminListReportsQuery : IRequest<PagedAdminReportResponse>
{
    public string? SearchTerm { get; init; }
    public string? Status { get; init; }
    public bool? IsLocked { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? ProjectId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class AdminListReportsHandler(WinnowDbContext dbContext) : IRequestHandler<AdminListReportsQuery, PagedAdminReportResponse>
{
    public async Task<PagedAdminReportResponse> Handle(AdminListReportsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
#pragma warning disable CA1304, CA1311, CA1862
            var search = request.SearchTerm.Trim().ToLowerInvariant();
            if (search.StartsWith("r-"))
            {
                search = search.Substring(2);
            }

            if (Guid.TryParse(search, out var searchGuid))
            {
                query = query.Where(r =>
                    r.Id == searchGuid ||
                    r.Title.ToLower().Contains(search));
            }
            else
            {
                query = query.Where(r =>
                    r.Title.ToLower().Contains(search) ||
                    r.Id.ToString().Contains(search));
            }
#pragma warning restore CA1304, CA1311, CA1862
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ReportStatus.FromName(request.Status);
            query = query.Where(r => r.Status == status);
        }

        if (request.IsLocked.HasValue)
        {
            query = query.Where(r => r.IsLocked == request.IsLocked.Value);
        }

        if (request.OrganizationId.HasValue)
        {
            query = query.Where(r => r.OrganizationId == request.OrganizationId.Value);
        }

        if (request.ProjectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == request.ProjectId.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and sorting
        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReportSummary
            {
                Id = r.Id,
                Title = r.Title,
                Status = r.Status.Name,
                IsLocked = r.IsLocked,
                IsOverage = r.IsOverage,
                CreatedAt = r.CreatedAt,
                OrganizationId = r.OrganizationId,

                OrganizationName = dbContext.Organizations
                    .IgnoreQueryFilters()
                    .Where(o => o.Id == r.OrganizationId)
                    .Select(o => o.Name)
                    .FirstOrDefault() ?? "Unknown Organization",

                ProjectId = r.ProjectId,
                ProjectName = dbContext.Projects
                    .IgnoreQueryFilters()
                    .Where(p => p.Id == r.ProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefault() ?? "Unknown Project"
            })
            .ToListAsync(cancellationToken);

        return new PagedAdminReportResponse
        {
            Items = reports,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
