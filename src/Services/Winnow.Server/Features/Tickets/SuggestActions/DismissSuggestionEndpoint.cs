using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;
using Winnow.Server.Services.Ai;
using Winnow.Server.Infrastructure.MultiTenancy; // This was the missing one

namespace Winnow.Server.Features.Tickets.SuggestActions;

public class DismissSuggestionRequest
{
    public bool RejectMatch { get; set; }
}

public class DismissSuggestionEndpoint(WinnowDbContext db, INegativeMatchCache negativeCache, ITenantContext tenantContext) : Endpoint<DismissSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{Id}/dismiss-suggestion");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DismissSuggestionRequest req, CancellationToken ct)
    {
        var ticketId = Route<Guid>("Id");
        var ticket = await db.Tickets.FindAsync([ticketId], ct);
        
        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // If explicitly rejected (Teaching the AI), record it in the negative cache
        if (req.RejectMatch && ticket.SuggestedParentId.HasValue)
        {
            var tenantId = tenantContext.TenantId ?? "default";
            negativeCache.MarkAsMismatch(tenantId, ticket.Id, ticket.SuggestedParentId.Value);
        }

        // Clear the suggestion
        ticket.SuggestedParentId = null;
        ticket.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Suggestion dismissed." }, ct);
    }
}
