using FastEndpoints;
using FluentValidation;
using MassTransit;

namespace Winnow.Server.Features.Tickets.Create;

public class CreateTicketRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
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
        var ticket = new Entities.Ticket
        {
            Title = req.Title,
            Description = req.Description,
            CreatedAt = DateTime.UtcNow
        };

        // Ensure DB exists (Demo purposes, ideally migration)
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(ct);

        await publishEndpoint.Publish(new TicketCreatedEvent
        {
            TicketId = ticket.Id,
            Title = req.Title,
            Description = req.Description,
            CreatedAt = ticket.CreatedAt,
            TenantId = tenantContext.TenantId
        }, ct);

        HttpContext.Response.StatusCode = 202;
        await HttpContext.Response.CompleteAsync();
    }
}
