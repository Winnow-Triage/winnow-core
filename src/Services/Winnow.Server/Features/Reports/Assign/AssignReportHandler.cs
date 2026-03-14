using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Assign;

public record AssignReportCommand(Guid Id, Guid ProjectId, string? AssignedTo) : IRequest<AssignReportResult>;

public record AssignReportResult(bool IsSuccess, string? Message = null, string? ErrorMessage = null, int? StatusCode = null);

public class AssignReportHandler(WinnowDbContext db) : IRequestHandler<AssignReportCommand, AssignReportResult>
{
    public async Task<AssignReportResult> Handle(AssignReportCommand request, CancellationToken cancellationToken)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.ProjectId == request.ProjectId, cancellationToken);

        if (report == null)
        {
            return new AssignReportResult(false, null, "Report not found", 404);
        }

        report.AssignTo(request.AssignedTo);

        await db.SaveChangesAsync(cancellationToken);

        return new AssignReportResult(true, $"Report assigned to {request.AssignedTo ?? "Unassigned"}");
    }
}
