using MediatR;
using Microsoft.EntityFrameworkCore;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Organizations.Invitations;

public record GetInvitationDetailsQuery(string Token) : IRequest<GetInvitationDetailsResult>;

public record GetInvitationDetailsResult(bool IsSuccess, GetInvitationDetailsResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetInvitationDetailsHandler(WinnowDbContext db) : IRequestHandler<GetInvitationDetailsQuery, GetInvitationDetailsResult>
{
    public async Task<GetInvitationDetailsResult> Handle(GetInvitationDetailsQuery request, CancellationToken cancellationToken)
    {
        var invitation = await db.OrganizationInvitations
            .Join(db.Organizations, oi => oi.OrganizationId, o => o.Id, (oi, o) => new { oi, o })
            .FirstOrDefaultAsync(x => x.oi.Token == request.Token, cancellationToken);

        if (invitation == null)
        {
            return new GetInvitationDetailsResult(false, null, "Invitation not found", 404);
        }

        if (invitation.oi.ExpiresAt < DateTime.UtcNow)
        {
            return new GetInvitationDetailsResult(false, null, "Invitation expired", 410);
        }

        var data = new GetInvitationDetailsResponse
        {
            Email = invitation.oi.Email.Value,
            OrganizationName = invitation.o.Name
        };

        return new GetInvitationDetailsResult(true, data);
    }
}
