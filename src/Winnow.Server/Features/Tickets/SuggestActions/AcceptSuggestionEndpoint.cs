using FastEndpoints;
using Winnow.Server.Features.Shared;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Tickets.SuggestActions;

public class AcceptSuggestionRequest
{
    public Guid Id { get; set; }
}

public class AcceptSuggestionEndpoint(WinnowDbContext db) : Endpoint<AcceptSuggestionRequest, ActionResponse>
{
    public override void Configure()
    {
        Post("/tickets/{Id}/accept-suggestion");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AcceptSuggestionRequest req, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([req.Id], ct);
        if (ticket == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (ticket.SuggestedParentId == null)
        {
            AddError("Ticket has no suggested parent to accept.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        // Apply the suggestion
        ticket.ParentTicketId = ticket.SuggestedParentId;
        ticket.Status = "Duplicate";
        ticket.ConfidenceScore = ticket.SuggestedConfidenceScore;

        // Clear the suggestion
        ticket.SuggestedParentId = null;
        ticket.SuggestedConfidenceScore = null;

        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new ActionResponse { Message = "Suggestion accepted successfully." }, ct);
    }
}
