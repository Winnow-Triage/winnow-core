using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Admin.Organizations.RemoveMember;

public class RemoveOrganizationMemberRequest
{
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public sealed class RemoveOrganizationMemberEndpoint(IMediator mediator) : Endpoint<RemoveOrganizationMemberRequest>
{
    public override void Configure()
    {
        Delete("/admin/organizations/{organizationId}/members/{userId}");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Remove a user from an organization (SuperAdmin only)";
            s.Description = "Removes the specified user's membership from an organization. This is a powerful administrative action.";
            s.Response(204, "Member removed successfully");
            s.Response(404, "Membership not found");
        });
    }

    public override async Task HandleAsync(RemoveOrganizationMemberRequest req, CancellationToken ct)
    {
        var command = new RemoveOrganizationMemberCommand
        {
            OrganizationId = req.OrganizationId,
            UserId = req.UserId
        };

        try
        {
            await mediator.Send(command, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

