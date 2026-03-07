using MassTransit;
using Winnow.Server.Domain.Reports;
using Winnow.Server.Features.Reports.Create;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Debug;

public class SimulateTrafficRequest : ProjectScopedRequest
{
    public int Count { get; set; } = 5;
    public string Topic { get; set; } = "Login Failure";
}

public class SimulateTrafficResponse
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class SimulateTrafficEndpoint(
    IPublishEndpoint publishEndpoint,
    WinnowDbContext dbContext,
    Services.Quota.IQuotaService quotaService,
    Services.Ai.IEmbeddingService embeddingService) : ProjectScopedEndpoint<SimulateTrafficRequest, SimulateTrafficResponse>
{
    public override void Configure()
    {
        Post("/debug/simulate-traffic");
        Summary(s =>
        {
            s.Summary = "Simulate traffic";
            s.Description = "Generates synthetic traffic/reports for testing purposes.";
            s.Response<SimulateTrafficResponse>(200, "Simulation started");
            s.Response(400, "Invalid project ID");
            s.Response(404, "Project not found or access denied");
        });
        Options(x => x.RequireAuthorization());
    }

    public override async Task HandleAsync(SimulateTrafficRequest req, CancellationToken ct)
    {
        var orgId = req.CurrentOrganizationId;
        var templates = GetTemplates(req.Topic);
        var random = new Random();

        var reportsToPublish = new List<ReportCreatedEvent>();

        var (isOverage, isLocked) = await quotaService.GetIngestionQuotaStatusAsync(orgId, ct);
        if (isLocked)
        {
            await quotaService.EnforceRetroactiveRansomAsync(orgId, ct);
        }

        for (int i = 0; i < req.Count; i++)
        {
            var template = templates[random.Next(templates.Count)];
            var title = $"{template.Title} {random.Next(1000, 9999)}";

            var textToEmbed = $"{title}\n{template.Description}";
            var embeddingFloats = await embeddingService.GetEmbeddingAsync(textToEmbed);

            var report = new Report(
                req.CurrentProjectId,
                orgId,
                title,
                template.Description,
                null
            );

            report.SetEmbedding(embeddingFloats);

            if (isOverage) report.MarkOverage();
            if (isLocked) report.Lock();

            dbContext.Reports.Add(report);

            reportsToPublish.Add(new ReportCreatedEvent
            {
                ReportId = report.Id,
                ProjectId = report.ProjectId,
                Title = report.Title,
                Message = report.Message,
                StackTrace = report.StackTrace,
                CreatedAt = report.CreatedAt,
                OrganizationId = orgId
            });
        }

        // Save all reports in a single transaction
        await dbContext.SaveChangesAsync(ct);

        foreach (var evt in reportsToPublish)
        {
            await publishEndpoint.Publish(evt, ct);
        }

        await Send.OkAsync(new SimulateTrafficResponse
        {
            Message = $"Simulated {req.Count} reports for topic '{req.Topic}' in project {req.CurrentProjectId}.",
            Count = req.Count
        }, ct);
    }

    private List<(string Title, string Description)> GetTemplates(string topic)
    {
        return topic switch
        {
            "Login Failure" =>
            [
                ("User cannot login", "Getting a 500 error when clicking the login button."),
                ("Login page unresponsive", "The login page hangs and eventually times out."),
                ("Invalid credentials error", "Users reporting valid credentials are rejected."),
                ("Password reset broken", "Clicking password reset link leads to 404."),
                ("SSO Login failing", "Google SSO button does nothing.")
            ],
            "Database Timeout" =>
            [
                ("Query timeout", "SQL timeout exception in the reporting module."),
                ("Connection pool exhausted", "Application crashing with connection pool errors."),
                ("Slow query performance", "Dashboard taking 30s to load due to DB wait."),
                ("Deadlock detected", "Transaction deadlock in payment processing."),
                ("DB CPU High", "Database CPU usage spiked to 100%.")
            ],
            "Payment Issue" =>
            [
                ("Payment declined", "User reports card declined despite valid funds."),
                ("Stripe webhook failure", "Webhook returning 400 Bad Request."),
                ("Double charge", "Customer charged twice for the same subscription."),
                ("Invoice not generated", "Monthly invoice failed to generate."),
                ("Currency conversion error", "Wrong currency symbol displayed in checkout.")
            ],
            _ =>
            [
                ("System Error", "Generic system error occurred."),
                ("Feature Request", "User wants dark mode."),
                ("Bug report", "Something is not working."),
                ("Performance issue", "App feels sluggish."),
                ("UI Glitch", "Button is misaligned.")
            ]
        };
    }
}
