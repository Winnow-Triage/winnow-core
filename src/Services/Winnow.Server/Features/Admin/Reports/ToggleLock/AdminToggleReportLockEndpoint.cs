using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Reports.ToggleLock;

public class AdminToggleReportLockRequest
{
    public Guid Id { get; set; }
}

public class AdminToggleReportLockResponse
{
    public Guid Id { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class AdminToggleReportLockEndpoint(IMediator mediator) : Endpoint<AdminToggleReportLockRequest, AdminToggleReportLockResponse>
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
        var command = new AdminToggleReportLockCommand { Id = req.Id };

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

