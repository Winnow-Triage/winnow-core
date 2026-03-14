using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.List;

public class ListReportsRequest : ProjectScopedRequest { }

/// <summary>
/// Summary of a report.
/// </summary>
/// <param name="Id">Unique identifier of the report.</param>
/// <param name="Title">Title of the report.</param>
/// <param name="Message">Brief message or description.</param>
/// <param name="StackTrace">Stack trace if available.</param>
/// <param name="Status">Current status of the report.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="ClusterId">ID of the cluster if grouped.</param>
/// <param name="ConfidenceScore">AI confidence score.</param>
/// <param name="Metadata">JSON metadata.</param>
/// <param name="IsOverage">Whether this report exceeded the free limits.</param>
/// <param name="IsLocked">Whether this report was held for ransom due to grace period breach.</param>
public record ReportDto(
    Guid Id,
    string Title,
    string Message,
    string? StackTrace,
    string Status,
    DateTime CreatedAt,
    Guid? ClusterId,
    double? ConfidenceScore,
    string? Metadata,
    bool IsOverage,
    bool IsLocked);

public sealed class ListReportsEndpoint(IMediator mediator) : ProjectScopedEndpoint<ListReportsRequest, List<ReportDto>>
{
    public override void Configure()
    {
        Get("/reports");
        Summary(s =>
        {
            s.Summary = "List all reports";
            s.Description = "Retrieves a list of reports for the project, optionally sorted by criticality or confidence.";
            s.Response<List<ReportDto>>(200, "List of reports");
            s.Response(400, "Invalid request");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(ListReportsRequest req, CancellationToken ct)
    {
        string sort = HttpContext.Request.Query["sort"].ToString();

        var query = new ListReportsQuery(req.CurrentProjectId, sort);
        var reports = await mediator.Send(query, ct);

        await Send.OkAsync(reports, ct);
    }
}
