using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Admin.Reports.ResetOverage;

public record AdminResetReportOverageCommand : IRequest<AdminResetReportOverageResponse>
{
    public Guid Id { get; init; }
}

public class AdminResetReportOverageHandler(WinnowDbContext dbContext) : IRequestHandler<AdminResetReportOverageCommand, AdminResetReportOverageResponse>
{
    public async Task<AdminResetReportOverageResponse> Handle(AdminResetReportOverageCommand request, CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
        {
            throw new InvalidOperationException("Report not found.");
        }

        report.AdminResetOverage();

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdminResetReportOverageResponse
        {
            Id = report.Id,
            IsOverage = report.IsOverage
        };
    }
}
