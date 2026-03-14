using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin.Reports.Get;

public record AdminGetReportQuery : IRequest<AdminReportResponse>
{
    public Guid Id { get; init; }
}

public class AdminGetReportHandler(WinnowDbContext dbContext) : IRequestHandler<AdminGetReportQuery, AdminReportResponse>
{
    public async Task<AdminReportResponse> Handle(AdminGetReportQuery request, CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
        {
            throw new InvalidOperationException("Report not found.");
        }

        return new AdminReportResponse
        {
            Id = report.Id,
            ProjectId = report.ProjectId,
            OrganizationId = report.OrganizationId,
            Title = report.Title,
            Status = report.Status.Name,
            IsLocked = report.IsLocked,
            IsOverage = report.IsOverage,
            CreatedAt = report.CreatedAt
        };
    }
}
