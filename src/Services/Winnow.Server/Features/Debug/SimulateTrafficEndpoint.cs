using System.Security.Claims;
using FastEndpoints;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Debug;

/// <summary>
/// Request to simulate traffic for debugging.
/// </summary>
public class SimulateTrafficRequest
{
    /// <summary>
    /// Number of events/reports to generate.
    /// </summary>
    public int Count { get; set; } = 5;

    /// <summary>
    /// Topic/Scenario to simulate (e.g., "Login Failure").
    /// </summary>
    public string Topic { get; set; } = "Login Failure";
}

/// <summary>
/// Response from simulation.
/// </summary>
public class SimulateTrafficResponse
{
    /// <summary>
    /// Result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of items generated.
    /// </summary>
    public int Count { get; set; }
}

public sealed class SimulateTrafficEndpoint(
    IPublishEndpoint publishEndpoint,
    WinnowDbContext dbContext,
    ITenantContext tenantContext,
    Winnow.Server.Services.Quota.IQuotaService quotaService,
    Winnow.Server.Services.Ai.IEmbeddingService embeddingService) : Endpoint<SimulateTrafficRequest, SimulateTrafficResponse>
{
    public override void Configure()
    {
        Post("/debug/simulate-traffic");
        Summary(s =>
        {
            s.Summary = "Simulate traffic";
            s.Description = "Generates synthetic traffic/reports for testing purposes.";
            s.Response<SimulateTrafficResponse>(200, "Simulation started");
            s.Response(401, "Unauthorized");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(SimulateTrafficRequest req, CancellationToken ct)
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

        // Validate user owns this project
        var userOwnsProject = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

        if (!userOwnsProject)
        {
            ThrowError("Project not found or access denied", 404);
        }

        var templates = GetTemplates(req.Topic);
        var random = new Random();

        await dbContext.Database.EnsureCreatedAsync(ct);

        var reportsToPublish = new List<ReportCreatedEvent>();

        for (int i = 0; i < req.Count; i++)
        {
            var currentOrgId = tenantContext.CurrentOrganizationId ?? Guid.Empty;

            var quotaStatus = await quotaService.GetIngestionQuotaStatusAsync(currentOrgId, ct);
            if (quotaStatus.isLocked)
            {
                await quotaService.EnforceRetroactiveRansomAsync(currentOrgId, ct);
            }

            var template = templates[random.Next(templates.Count)];
            var title = $"{template.Title} {random.Next(1000, 9999)}";

            // Generate embedding for the simulated report
            var textToEmbed = $"{title}\n{template.Description}";
            var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);
            var embeddingBytes = new byte[embeddingFloats.Length * sizeof(float)];
            Buffer.BlockCopy(embeddingFloats, 0, embeddingBytes, 0, embeddingBytes.Length);

            var report = new Entities.Report
            {
                Title = title,
                Message = template.Description,
                StackTrace = template.Description,
                CreatedAt = DateTime.UtcNow,
                Status = "New",
                ProjectId = projectId, // Use the validated project ID
                OrganizationId = currentOrgId,
                Embedding = embeddingBytes, // Include the embedding
                IsOverage = quotaStatus.isOverage,
                IsLocked = quotaStatus.isLocked
            };

            dbContext.Reports.Add(report);

            reportsToPublish.Add(new ReportCreatedEvent
            {
                ReportId = report.Id,
                ProjectId = report.ProjectId,
                Title = report.Title,
                Message = report.Message,
                StackTrace = report.StackTrace,
                CreatedAt = report.CreatedAt,
                TenantId = tenantContext.TenantId
            });

            await dbContext.SaveChangesAsync(ct);
        }

        foreach (var evt in reportsToPublish)
        {
            await publishEndpoint.Publish(evt, ct);
        }

        await Send.OkAsync(new SimulateTrafficResponse
        {
            Message = $"Simulated {req.Count} reports for topic '{req.Topic}' in project {projectId} (Tenant: {tenantContext.TenantId})",
            Count = req.Count
        }, cancellation: ct);
    }

    private List<(string Title, string Description)> GetTemplates(string topic)
    {
        return topic switch
        {
            "Login Failure" => new()
            {
                ("User cannot login", "Getting a 500 error when clicking the login button."),
                ("Login page unresponsive", "The login page hangs and eventually times out."),
                ("Invalid credentials error", "Users reporting valid credentials are rejected."),
                ("Password reset broken", "Clicking password reset link leads to 404."),
                ("SSO Login failing", "Google SSO button does nothing.")
            },
            "Database Timeout" => new()
            {
                ("Query timeout", "SQL timeout exception in the reporting module."),
                ("Connection pool exhausted", "Application crashing with connection pool errors."),
                ("Slow query performance", "Dashboard taking 30s to load due to DB wait."),
                ("Deadlock detected", "Transaction deadlock in payment processing."),
                ("DB CPU High", "Database CPU usage spiked to 100%.")
            },
            "Payment Issue" => new()
            {
                ("Payment declined", "User reports card declined despite valid funds."),
                ("Stripe webhook failure", "Webhook returning 400 Bad Request."),
                ("Double charge", "Customer charged twice for the same subscription."),
                ("Invoice not generated", "Monthly invoice failed to generate."),
                ("Currency conversion error", "Wrong currency symbol displayed in checkout.")
            },
            _ => new()
            {
                ("System Error", "Generic system error occurred."),
                ("Feature Request", "User wants dark mode."),
                ("Bug report", "Something is not working."),
                ("Performance issue", "App feels sluggish."),
                ("UI Glitch", "Button is misaligned.")
            }
        };
    }
}
