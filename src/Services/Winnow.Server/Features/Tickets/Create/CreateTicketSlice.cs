using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FastEndpoints;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Winnow.Server.Features.Tickets.Create;

public class CreateTicketRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? StackTrace { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class CreateTicketValidator : Validator<CreateTicketRequest>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty();
    }
}

public record TicketCreatedEvent
{
    public Guid TicketId { get; init; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public string? TenantId { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

public class ApiKeyPreProcessor : IPreProcessor<CreateTicketRequest>
{
    public Task PreProcessAsync(IPreProcessorContext<CreateTicketRequest> context, CancellationToken ct)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Winnow-Key", out var apiKey) || apiKey != "secret-key")
        {
            context.HttpContext.Response.StatusCode = 403;
            return context.HttpContext.Response.WriteAsync("Invalid API Key", ct);
        }
        return Task.CompletedTask;
    }
}

public class CreateTicketEndpoint(
    IPublishEndpoint publishEndpoint,
    Infrastructure.MultiTenancy.ITenantContext tenantContext,
    Infrastructure.Persistence.WinnowDbContext dbContext) : Endpoint<CreateTicketRequest>
{
    public override void Configure()
    {
        Post("/tickets");
        AllowAnonymous();
        PreProcessor<ApiKeyPreProcessor>();
        Description(b => b
            .Accepts<CreateTicketRequest>("application/json")
            .Produces(202));
    }

    public override async Task HandleAsync(CreateTicketRequest req, CancellationToken ct)
    {
        string? stackHash = null;
        if (!string.IsNullOrWhiteSpace(req.StackTrace))
        {
            // Normalize Stack Trace (Remove memory addresses e.g. 0x00007ff)
            var normalized = Regex.Replace(req.StackTrace, @"0x[0-9a-fA-F]+", "0xADDR");

            // MD5 Hash
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            stackHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        var ticket = new Entities.Ticket
        {
            Title = req.Title,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow,
            StackTraceHash = stackHash,
            MetadataJson = req.Metadata != null ? JsonSerializer.Serialize(req.Metadata) : null
        };

        // Ensure DB exists
        await dbContext.Database.EnsureCreatedAsync(ct);

        // Deterministic Match: If we have a stack hash, look for existing parent
        if (stackHash != null)
        {
            var existingMatch = await dbContext.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(t => t.StackTraceHash == stackHash, ct);

            if (existingMatch != null)
            {
                ticket.ParentTicketId = existingMatch.ParentTicketId ?? existingMatch.Id;
                ticket.Status = "Duplicate (StackHash)";
                ticket.ConfidenceScore = 1.0f;
            }
        }

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(ct);

        // Publish event (Clustering will only run semantic search if NOT already linked by StackHash)
        await publishEndpoint.Publish(new TicketCreatedEvent
        {
            TicketId = ticket.Id,
            Title = req.Title,
            Description = req.Description,
            CreatedAt = ticket.CreatedAt,
            TenantId = tenantContext.TenantId,
            Metadata = req.Metadata
        }, ct);

        HttpContext.Response.StatusCode = 202;
        await HttpContext.Response.CompleteAsync();
    }
}
