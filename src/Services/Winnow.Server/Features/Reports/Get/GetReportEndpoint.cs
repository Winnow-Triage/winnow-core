using FastEndpoints;
using MediatR;
using Winnow.Server.Features.Shared;

namespace Winnow.Server.Features.Reports.Get;

/// <summary>
/// Request to retrieve a single report.
/// </summary>
public class GetReportRequest : ProjectScopedRequest
{
    /// <summary>
    /// The unique identifier of the report to retrieve.
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
/// Detailed response containing report data, assets, and evidence.
/// </summary>
public class GetReportResponse
{
    /// <summary>
    /// The unique identifier of the report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The project this report belongs to.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// The title or subject of the report.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The main message or description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The stack trace associated with the report, if applicable.
    /// </summary>
    public string? StackTrace { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the report (e.g., Open, Closed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the report was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Cluster fields
    /// <summary>
    /// ID of the cluster this report belongs to.
    /// </summary>
    public Guid? ClusterId { get; set; }

    /// <summary>
    /// Username or ID of the user assigned to this report.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// AI-generated cluster summary.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// AI-calculated confidence score (0-1).
    /// </summary>
    public float? ConfidenceScore { get; set; }

    /// <summary>
    /// Criticality score (1-100).
    /// </summary>
    public int? CriticalityScore { get; set; }

    /// <summary>
    /// Reasoning behind the criticality score.
    /// </summary>
    public string? CriticalityReasoning { get; set; }

    /// <summary>
    /// AI-generated cluster title.
    /// </summary>
    public string? ClusterTitle { get; set; }

    /// <summary>
    /// Suggested cluster ID from analysis.
    /// </summary>
    public Guid? SuggestedClusterId { get; set; }

    /// <summary>
    /// Confidence score for the suggested cluster.
    /// </summary>
    public float? SuggestedConfidenceScore { get; set; }

    /// <summary>
    /// Summary from the suggested cluster.
    /// </summary>
    public string? SuggestedClusterSummary { get; set; }

    /// <summary>
    /// Title from the suggested cluster.
    /// </summary>
    public string? SuggestedClusterTitle { get; set; }

    /// <summary>
    /// JSON metadata string.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this report exceeded the free limits.
    /// </summary>
    public bool IsOverage { get; set; }

    /// <summary>
    /// Whether this report was held for ransom due to grace period breach.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// URL or path to a screenshot.
    /// </summary>
    public string? Screenshot { get; set; }

    /// <summary>
    /// External link related to the report.
    /// </summary>
    public Uri? ExternalUrl { get; set; }

    /// <summary>
    /// List of associated assets/files.
    /// </summary>
    public List<AssetDto> Assets { get; set; } = [];

    /// <summary>
    /// Related reports providing evidence or context.
    /// </summary>
    public List<RelatedReportDto> Evidence { get; set; } = [];
}

/// <summary>
/// Represents a file asset attached to a report.
/// </summary>
public class AssetDto
{
    /// <summary>
    /// Unique identifier for the asset.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Scan status: Pending, Clean, Infected, Failed.
    /// </summary>
    public string Status { get; set; } = string.Empty; // Pending, Clean, Infected, Failed

    /// <summary>
    /// Temporary download URL (if clean).
    /// </summary>
    public Uri? DownloadUrl { get; set; } // Presigned URL, only for Clean assets

    /// <summary>
    /// When the asset was uploaded.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the asset was scanned by antivirus.
    /// </summary>
    public DateTime? ScannedAt { get; set; }
}

/// <summary>
/// A related report used as evidence.
/// </summary>
public class RelatedReportDto
{
    /// <summary>
    /// Unique identifier of the related report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Brief message from the related report.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Status of the related report.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the related report was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Relevance score to the main report.
    /// </summary>
    public float? ConfidenceScore { get; set; }
}

public sealed class GetReportEndpoint(IMediator mediator) : ProjectScopedEndpoint<GetReportRequest, GetReportResponse>
{
    public override void Configure()
    {
        Get("/reports/{id:guid}");
        Description(x => x.WithName("GetReport"));
        Summary(s =>
        {
            s.Summary = "Retrieve a specific report";
            s.Description = "Fetches a report by its ID, including metadata, assets, and evidence.";
            s.Response<GetReportResponse>(200, "The requested report");
            s.Response(404, "Report not found");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(GetReportRequest req, CancellationToken ct)
    {
        var query = new GetReportQuery(req.CurrentOrganizationId, req.Id, req.CurrentProjectId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == 404)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
