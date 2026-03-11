using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Organizations.Invitations;

public class GetInvitationDetailsRequest
{
    public string Token { get; set; } = string.Empty;
}

public class GetInvitationDetailsResponse
{
    public string Email { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

public sealed class GetInvitationDetailsEndpoint(WinnowDbContext db) : Endpoint<GetInvitationDetailsRequest, GetInvitationDetailsResponse>
{
    public override void Configure()
    {
        Get("/invitations/{token}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetInvitationDetailsRequest req, CancellationToken ct)
    {
        var invitation = await db.OrganizationInvitations
            .Join(db.Organizations, oi => oi.OrganizationId, o => o.Id, (oi, o) => new { oi, o })
            .FirstOrDefaultAsync(x => x.oi.Token == req.Token, ct);

        if (invitation == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (invitation.oi.ExpiresAt < DateTime.UtcNow)
        {
            await Send.ErrorsAsync(410, ct); // Gone
            return;
        }

        await Send.OkAsync(new GetInvitationDetailsResponse
        {
            Email = invitation.oi.Email.Value,
            OrganizationName = invitation.o.Name
        }, ct);
    }
}
