using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Reports.ResetOverage;

public class AdminResetReportOverageRequest
{
    public Guid Id { get; set; }
}

public class AdminResetReportOverageResponse
{
    public Guid Id { get; set; }
    public bool IsOverage { get; set; }
}

public sealed class AdminResetReportOverageEndpoint(IMediator mediator) : Endpoint<AdminResetReportOverageRequest, AdminResetReportOverageResponse>
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
        var command = new AdminResetReportOverageCommand { Id = req.Id };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

