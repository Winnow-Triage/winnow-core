using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.SuggestActions;

public class DismissSuggestionRequest
{
    public Guid Id { get; set; }
}

public class DismissSuggestionEndpoint(WinnowDbContext db) : Endpoint<DismissSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{Id}/dismiss-suggestion");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DismissSuggestionRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([req.Id], ct);
        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Clear the suggestion
        ticket.SuggestedParentId = null;
        ticket.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Suggestion dismissed." }, ct);
    }
}
