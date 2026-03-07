using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class AdminResetReportOverageRequest
{
    public Guid Id { get; set; }
}

public class AdminResetReportOverageResponse
{
    public Guid Id { get; set; }
    public bool IsOverage { get; set; }
}

public sealed class AdminResetReportOverageEndpoint(WinnowDbContext dbContext) : Endpoint<AdminResetReportOverageRequest, AdminResetReportOverageResponse>
{
    public override void Configure()
    {
        Post("/admin/reports/{id}/reset-overage");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Reset report overage status (SuperAdmin only)";
            s.Description = "Resets the IsOverage status of a report to false bypassing tenant isolation.";
            s.Response<AdminResetReportOverageResponse>(200, "Success");
            s.Response(404, "Report not found");
        });
    }

    public override async Task HandleAsync(AdminResetReportOverageRequest req, CancellationToken ct)
    {
        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        report.AdminResetOverage();

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new AdminResetReportOverageResponse
        {
            Id = report.Id,
            IsOverage = report.IsOverage
        }, ct);
    }
}
