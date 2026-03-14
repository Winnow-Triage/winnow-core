using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Admin.Reports.Get;

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

public sealed class AdminGetReportEndpoint(IMediator mediator) : Endpoint<AdminGetReportRequest, AdminReportResponse>
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
        var query = new AdminGetReportQuery { Id = req.Id };

        try
        {
            var result = await mediator.Send(query, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

