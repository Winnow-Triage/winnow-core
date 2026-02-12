using FastEndpoints;
using MassTransit;
using Winnow.Server.Features.Tickets.Create;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Debug;

public class SimulateTrafficRequest
{
    public int Count { get; set; } = 5;
    public string Topic { get; set; } = "Login Failure";
}

public class SimulateTrafficResponse
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SimulateTrafficEndpoint(
    IPublishEndpoint publishEndpoint,
    WinnowDbContext dbContext,
    ITenantContext tenantContext) : Endpoint<SimulateTrafficRequest, SimulateTrafficResponse>
{
    public override void Configure()
    {
        Post("/debug/simulate-traffic");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SimulateTrafficRequest req, CancellationToken ct)
    {
        // Removed hardcoded tenant check. We rely on Middleware now.
        // var contextTenant = tenantContext.TenantId; 

        var templates = GetTemplates(req.Topic);
        var random = new Random();

        // Ensure DB exists
        await dbContext.Database.EnsureCreatedAsync(ct);

        var ticketsToPublish = new List<TicketCreatedEvent>();

        for (int i = 0; i < req.Count; i++)
        {
            var template = templates[random.Next(templates.Count)];
            var title = template.Title + $" {random.Next(1000, 9999)}";

            var ticket = new Entities.Ticket
            {
                Title = title,
                Description = template.Description,
                CreatedAt = DateTime.UtcNow,
                Status = "New"
            };

            dbContext.Tickets.Add(ticket);

            ticketsToPublish.Add(new TicketCreatedEvent
            {
                TicketId = ticket.Id,
                TenantId = tenantContext.TenantId, // Use the actual context!
                Title = ticket.Title,
                Description = ticket.Description,
                CreatedAt = ticket.CreatedAt
            });
        }

        await dbContext.SaveChangesAsync(ct);

        foreach (var evt in ticketsToPublish)
        {
            await publishEndpoint.Publish(evt, ct);
        }

        await Send.OkAsync(new SimulateTrafficResponse
        {
            Message = $"Simulated {req.Count} tickets for topic '{req.Topic}' (Tenant: {tenantContext.TenantId})",
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
            _ => new() // Default / Generic
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
