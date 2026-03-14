using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Reports.Export;

/// <summary>
/// Request to export a report via an integration.
/// </summary>
public class ExportReportRequest
{
    /// <summary>
    /// ID of the integration configuration to use.
    /// </summary>
    public Guid ConfigId { get; set; }
}

/// <summary>
/// Response containing the external URL of the exported report.
/// </summary>
public class ExportReportResponse
{
    /// <summary>
    /// URL of the exported issue in the external system.
    /// </summary>
    public Uri ExternalUrl { get; set; } = default!;
}

public sealed class ExportReportEndpoint(IMediator mediator) : Endpoint<ExportReportRequest>
{
    public override void Configure()
    {
        Post("/reports/{Id}/export");
        Policies("RequireVerifiedEmail");
        Summary(s =>
        {
            s.Summary = "Export a report";
            s.Description = "Exports a report to an external system (e.g., Jira, GitHub) using a configured integration.";
            s.Response<ExportReportResponse>(200, "Report exported successfully");
            s.Response(404, "Report not found");
            s.Response(400, "Export failed");
        });
        Description(d => d.WithDescription("Email verification required to perform this action."));
    }

    public override async Task HandleAsync(ExportReportRequest req, CancellationToken ct)
    {
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) ThrowError("Unauthorized", 401);

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        var reportId = Route<Guid>("Id");

        var command = new ExportReportCommand(reportId, projectId, userId, req.ConfigId);
        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            if (result.StatusCode == 400)
            {
                AddError(result.ErrorMessage!);
                ThrowIfAnyErrors();
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
