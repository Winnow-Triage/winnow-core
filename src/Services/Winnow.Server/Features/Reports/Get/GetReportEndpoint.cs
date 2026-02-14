using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Get;

public class GetReportRequest
{
    public Guid Id { get; set; }
}

public class GetReportResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Clustering/Legacy fields
    public Guid? ParentReportId { get; set; }
    public string? AssignedTo { get; set; }
    public string? Summary { get; set; }
    public float? ConfidenceScore { get; set; }
    public int? CriticalityScore { get; set; }
    public string? CriticalityReasoning { get; set; }
    public string? ParentReportMessage { get; set; } // Was ParentTicketTitle
    public Guid? SuggestedParentId { get; set; }
    public float? SuggestedConfidenceScore { get; set; }
    public string? SuggestedParentMessage { get; set; } // Was SuggestedParentTitle
    public string? Metadata { get; set; }
    public string? Screenshot { get; set; }
    public string? ExternalUrl { get; set; }

    public List<RelatedReportDto> Evidence { get; set; } = [];
}

public class RelatedReportDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public float? ConfidenceScore { get; set; }
}

public class GetReportEndpoint(WinnowDbContext db) : Endpoint<GetReportRequest, GetReportResponse>
{
    public override void Configure()
    {
        Get("/reports/{id}");
        AllowAnonymous();
        Description(x => x.WithName("GetReport"));
    }

    public override async Task HandleAsync(GetReportRequest req, CancellationToken ct)
    {
        var report = await db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var evidence = new List<RelatedReportDto>();

        if (report.ParentReportId == null)
        {
            // cluster parent
            var children = await db.Reports
                .AsNoTracking()
                .Where(t => t.ParentReportId == report.Id)
                .Select(t => new RelatedReportDto
                {
                    Id = t.Id,
                    Message = t.Message,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    ConfidenceScore = t.ConfidenceScore
                })
                .ToListAsync(ct);

            evidence.AddRange(children);
        }

        string? parentReportMessage = null;
        if (report.ParentReportId != null)
        {
            parentReportMessage = await db.Reports
                .AsNoTracking()
                .Where(t => t.Id == report.ParentReportId)
                .Select(t => t.Message)
                .FirstOrDefaultAsync(ct);
        }

        string? suggestedParentMessage = null;
        if (report.SuggestedParentId != null)
        {
            suggestedParentMessage = await db.Reports
                .AsNoTracking()
                .Where(t => t.Id == report.SuggestedParentId)
                .Select(t => t.Message)
                .FirstOrDefaultAsync(ct);
        }

        await Send.OkAsync(new GetReportResponse
        {
            Id = report.Id,
            Title = report.Title,
            Message = report.Message,
            StackTrace = report.StackTrace,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
            ParentReportId = report.ParentReportId,
            AssignedTo = report.AssignedTo,
            Summary = report.Summary,
            ConfidenceScore = report.ConfidenceScore,
            CriticalityScore = report.CriticalityScore,
            CriticalityReasoning = report.CriticalityReasoning,
            ParentReportMessage = parentReportMessage,
            SuggestedParentId = report.SuggestedParentId,
            SuggestedConfidenceScore = report.SuggestedConfidenceScore,
            SuggestedParentMessage = suggestedParentMessage,
            Metadata = report.Metadata,
            Screenshot = report.Screenshot,
            ExternalUrl = report.ExternalUrl,
            Evidence = evidence
        }, ct);
    }
}
