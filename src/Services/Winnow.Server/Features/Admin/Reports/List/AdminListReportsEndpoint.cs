using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Reports.List;

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

public sealed class AdminListReportsEndpoint(IMediator mediator) : Endpoint<AdminListReportsRequest, PagedAdminReportResponse>
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
        var query = new AdminListReportsQuery
        {
            SearchTerm = req.SearchTerm,
            Status = req.Status,
            IsLocked = req.IsLocked,
            OrganizationId = req.OrganizationId,
            ProjectId = req.ProjectId,
            Page = req.Page,
            PageSize = req.PageSize
        };

        var result = await mediator.Send(query, ct);
        await Send.OkAsync(result, ct);
    }
}

