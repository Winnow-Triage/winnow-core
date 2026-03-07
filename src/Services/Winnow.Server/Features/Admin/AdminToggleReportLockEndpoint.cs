using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class AdminToggleReportLockRequest
{
    public Guid Id { get; set; }
}

public class AdminToggleReportLockResponse
{
    public Guid Id { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class AdminToggleReportLockEndpoint(WinnowDbContext dbContext) : Endpoint<AdminToggleReportLockRequest, AdminToggleReportLockResponse>
{
    public override void Configure()
    {
        Post("/admin/reports/{id}/toggle-lock");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Toggle report lock status (SuperAdmin only)";
            s.Description = "Toggles the lock status of a report bypassing tenant isolation.";
            s.Response<AdminToggleReportLockResponse>(200, "Success");
            s.Response(404, "Report not found");
        });
    }

    public override async Task HandleAsync(AdminToggleReportLockRequest req, CancellationToken ct)
    {
        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (report.IsLocked)
        {
            report.Unlock();
        }
        else
        {
            report.Lock();
        }

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new AdminToggleReportLockResponse
        {
            Id = report.Id,
            IsLocked = report.IsLocked
        }, ct);
    }
}
