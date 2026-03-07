using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class AdminListReportsRequest
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public bool? IsLocked { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? ProjectId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AdminReportSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool IsOverage { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

public class PagedAdminReportResponse
{
    public List<AdminReportSummary> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class AdminListReportsEndpoint(WinnowDbContext dbContext) : Endpoint<AdminListReportsRequest, PagedAdminReportResponse>
{
    public override void Configure()
    {
        Get("/admin/reports");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "List reports (SuperAdmin only)";
            s.Description = "Returns a paginated list of reports bypassing tenant isolation. Supports filtering by search term, status, lock status, and organization.";
            s.Response<PagedAdminReportResponse>(200, "Success");
            s.Response(401, "Unauthorized (missing or invalid JWT)");
            s.Response(403, "Forbidden (user is not SuperAdmin)");
        });
    }

    public override async Task HandleAsync(AdminListReportsRequest req, CancellationToken ct)
    {
        var query = dbContext.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(req.SearchTerm))
        {
#pragma warning disable CA1304, CA1311, CA1862
            var search = req.SearchTerm.Trim().ToLowerInvariant();
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

        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            var status = ReportStatus.FromName(req.Status);
            query = query.Where(r => r.Status == status);
        }

        if (req.IsLocked.HasValue)
        {
            query = query.Where(r => r.IsLocked == req.IsLocked.Value);
        }

        if (req.OrganizationId.HasValue)
        {
            query = query.Where(r => r.OrganizationId == req.OrganizationId.Value);
        }

        if (req.ProjectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == req.ProjectId.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply pagination and sorting
        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
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
            .ToListAsync(ct);

        var response = new PagedAdminReportResponse
        {
            Items = reports,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };

        await Send.OkAsync(response, ct);
    }
}
