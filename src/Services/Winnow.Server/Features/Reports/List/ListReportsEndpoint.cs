using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.List;

public record ReportDto(
    Guid Id,
    string Title,
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
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Get user ID from JWT
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("Unauthorized", 401);
        }

        // Get project ID from header
        if (!HttpContext.Request.Headers.TryGetValue("X-Project-ID", out var projectIdHeader))
        {
            ThrowError("Project ID is required in X-Project-ID header", 400);
        }

        if (!Guid.TryParse(projectIdHeader, out var projectId))
        {
            ThrowError("Invalid Project ID format", 400);
        }

        // Validate user owns this project
        var userOwnsProject = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);
        
        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        string sort = HttpContext.Request.Query["sort"].ToString();
        if (string.IsNullOrEmpty(sort)) sort = "newest";

        var query = dbContext.Reports.AsNoTracking()
            .Where(r => r.ProjectId == projectId);

        query = sort switch
        {
            "criticality" => query.OrderByDescending(r => r.CriticalityScore).ThenByDescending(r => r.CreatedAt),
            "confidence" => query.OrderByDescending(r => r.ConfidenceScore).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var reports = await query
            .Select(r => new ReportDto(
                r.Id,
                r.Title,
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
