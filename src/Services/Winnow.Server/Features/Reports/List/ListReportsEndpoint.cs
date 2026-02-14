using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.List;

public record ReportDto(
    Guid Id, 
    string Message, 
    string? StackTrace, 
    string Status, 
    DateTime CreatedAt, 
    Guid? ParentReportId, 
    float? ConfidenceScore, 
    int? CriticalityScore, 
    string? Metadata);

public class ListReportsEndpoint(WinnowDbContext dbContext) : EndpointWithoutRequest<List<ReportDto>>
{
    public override void Configure()
    {
        Get("/reports");
        AllowAnonymous(); 
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        string sort = HttpContext.Request.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "newest";

        var query = dbContext.Reports.AsNoTracking();

        query = sort switch
        {
            "criticality" => query.OrderByDescending(r => r.CriticalityScore).ThenByDescending(r => r.CreatedAt),
            "confidence" => query.OrderByDescending(r => r.ConfidenceScore).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var reports = await query
            .Select(r => new ReportDto(
                r.Id, 
                r.Message, 
                r.StackTrace, 
                r.Status, 
                r.CreatedAt, 
                r.ParentReportId, 
                r.ConfidenceScore, 
                r.CriticalityScore, 
                r.Metadata))
            .ToListAsync(ct);

        await Send.OkAsync(reports, ct);
    }
}
