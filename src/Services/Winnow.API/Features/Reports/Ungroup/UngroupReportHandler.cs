using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Ai;

using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Reports.Ungroup;

[RequirePermission("reports:write")]
public record UngroupReportCommand(Guid CurrentOrganizationId, Guid Id, Guid ProjectId) : IRequest<UngroupReportResult>, IOrgScopedRequest;

public record UngroupReportResult(bool IsSuccess, string? Message = null, string? ErrorMessage = null, int? StatusCode = null);

public class UngroupReportHandler(WinnowDbContext db, IClusterService clusterService) : IRequestHandler<UngroupReportCommand, UngroupReportResult>
{
    public async Task<UngroupReportResult> Handle(UngroupReportCommand request, CancellationToken cancellationToken)
    {
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.ProjectId == request.ProjectId, cancellationToken);

        if (report == null)
        {
            return new UngroupReportResult(false, null, "Report not found", 404);
        }

        if (report.ClusterId == null)
        {
            return new UngroupReportResult(false, null, "Report is not grouped.", 400);
        }

        var oldClusterId = report.ClusterId;
        report.RemoveFromCluster();
        report.ChangeStatus(ReportStatus.Open);

        await db.SaveChangesAsync(cancellationToken);

        if (oldClusterId != null)
        {
            await clusterService.RecalculateCentroidAsync(oldClusterId.Value, cancellationToken);
        }

        return new UngroupReportResult(true, "Report ungrouped successfully.");
    }
}
