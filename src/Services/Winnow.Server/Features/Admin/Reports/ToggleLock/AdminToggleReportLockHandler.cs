using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Reports.ToggleLock;

public record AdminToggleReportLockCommand : IRequest<AdminToggleReportLockResponse>
{
    public Guid Id { get; init; }
}

public class AdminToggleReportLockHandler(WinnowDbContext dbContext) : IRequestHandler<AdminToggleReportLockCommand, AdminToggleReportLockResponse>
{
    public async Task<AdminToggleReportLockResponse> Handle(AdminToggleReportLockCommand request, CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
        {
            throw new InvalidOperationException("Report not found.");
        }

        if (report.IsLocked)
        {
            report.Unlock();
        }
        else
        {
            report.Lock();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdminToggleReportLockResponse
        {
            Id = report.Id,
            IsLocked = report.IsLocked
        };
    }
}
