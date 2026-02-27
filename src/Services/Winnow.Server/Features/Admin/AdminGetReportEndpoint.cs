using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class AdminGetReportRequest
{
    public Guid Id { get; set; }
}

public class AdminReportResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool IsOverage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AdminGetReportEndpoint(WinnowDbContext dbContext) : Endpoint<AdminGetReportRequest, AdminReportResponse>
{
    public override void Configure()
    {
        Get("/admin/reports/{id}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Get report details (SuperAdmin only)";
            s.Description = "Returns details for a specific report bypassing tenant isolation. Used for debugging and support.";
            s.Response<AdminReportResponse>(200, "Success");
            s.Response(404, "Report not found");
        });
    }

    public override async Task HandleAsync(AdminGetReportRequest req, CancellationToken ct)
    {
        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var response = new AdminReportResponse
        {
            Id = report.Id,
            ProjectId = report.ProjectId,
            OrganizationId = report.OrganizationId,
            Title = report.Title,
            Status = report.Status,
            IsLocked = report.IsLocked,
            IsOverage = report.IsOverage,
            CreatedAt = report.CreatedAt
        };

        await Send.OkAsync(response, ct);
    }
}
