using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Reports.Ungroup;

public class UngroupReportRequest
{
    public Guid Id { get; set; }
}

public class UngroupReportEndpoint(WinnowDbContext db) : Endpoint<UngroupReportRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/reports/{id}/ungroup");
    }

    public override async Task HandleAsync(UngroupReportRequest req, CancellationToken ct)
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
        var userOwnsProject = await db.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);
        
        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == req.Id && r.ProjectId == projectId, ct);

        if (report == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (report.ParentReportId == null)
        {
            ThrowError("Report is not grouped.");
        }

        report.ParentReportId = null;
        report.Status = "New"; 

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new ActionResponse { Message = "Report ungrouped successfully." }, ct);
    }
}
