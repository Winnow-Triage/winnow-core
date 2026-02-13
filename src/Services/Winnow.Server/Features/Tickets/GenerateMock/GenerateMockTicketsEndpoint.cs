using System.Text.Json;
using FastEndpoints;
using MassTransit;
using Microsoft.SemanticKernel;
using Winnow.Server.Entities;
using Winnow.Server.Features.Tickets.Create;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.GenerateMock;

public class GenerateMockTicketsRequest
{
    public int Count { get; set; } = 5;
    public string? Scenario { get; set; }
}

public class GenerateMockTicketsEndpoint(
    Kernel kernel,
    WinnowDbContext db,
    IPublishEndpoint publishEndpoint,
    ITenantContext tenantContext,
    ILogger<GenerateMockTicketsEndpoint> logger) : Endpoint<GenerateMockTicketsRequest>
{
    public override void Configure()
    {
        Post("/tickets/generate-mock");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GenerateMockTicketsRequest req, CancellationToken ct)
    {
        var prompt = $$"""
            Generate {{req.Count}} realistic technical support tickets for a software application.
            Scenario context: {{req.Scenario ?? "General SaaS application issues"}}

            Each ticket must have a Title and a Description.
            Return the result as a JSON array of objects with "title" and "description" properties.
            IMPORTANT: String values MUST be on a single line. Use literal \n (backslash + n) for any newlines inside strings.
            
            Example output format:
            [
              { "title": "Example Issue", "description": "Line 1\\nLine 2" }
            ]
            """;

        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);
        var json = result.ToString();

        // 1. Sanitize JSON (LLMs often wrap in markdown code blocks or add prefixes)
        int firstBracket = json.IndexOf('[');
        int lastBracket = json.LastIndexOf(']');

        if (firstBracket != -1 && lastBracket != -1 && lastBracket > firstBracket)
        {
            json = json.Substring(firstBracket, lastBracket - firstBracket + 1);
        }
        else
        {
            logger.LogWarning("LLM output did not contain a valid JSON array: {Json}", json);
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };
            var mockTickets = JsonSerializer.Deserialize<List<MockTicketDto>>(json, options);

            if (mockTickets == null)
            {
                logger.LogWarning("LLM returned empty or invalid JSON: {Json}", json);
                throw new Exception("Failed to deserialize mock tickets.");
            }

            foreach (var dt in mockTickets)
            {
                var ticket = new Ticket
                {
                    Title = dt.Title,
                    Description = dt.Description,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow
                };

                db.Tickets.Add(ticket);
                await db.SaveChangesAsync(ct);

                // Publish event so clustering logic runs
                await publishEndpoint.Publish(new TicketCreatedEvent
                {
                    TicketId = ticket.Id,
                    Title = ticket.Title,
                    Description = ticket.Description,
                    CreatedAt = ticket.CreatedAt,
                    TenantId = tenantContext.TenantId ?? "default"
                }, ct);
            }

            await Send.OkAsync(new { Message = $"Generated {mockTickets.Count} tickets." }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GenerateMockTicketsEndpoint. JSON was: {Json}", json);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }

    private record MockTicketDto(string Title, string Description);
}
