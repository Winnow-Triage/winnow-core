using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Admin.Organizations.AddMember;

public class AddOrganizationMemberRequest
{
    public Guid OrganizationId { get; set; }
    public string? UserId { get; set; } // If null, defaults to the current authenticated superadmin
    public string Role { get; set; } = "owner";
}

public class AddOrganizationMemberResponse
{
    public Guid MembershipId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Admin endpoint to manually add a user to an organization.
/// Primarily used by SuperAdmins to grant themselves or others access to a tenant.
/// </summary>
public sealed class AddOrganizationMemberEndpoint(IMediator mediator) : Endpoint<AddOrganizationMemberRequest, AddOrganizationMemberResponse>
{
    public override void Configure()
    {
        Post("/admin/organizations/{organizationId}/members");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Add a member to an organization (SuperAdmin only)";
            s.Description = "Grants a user access to an organization with a specific role. If UserId is omitted, it defaults to the current superadmin.";
            s.Response<AddOrganizationMemberResponse>(200, "Member added successfully");
            s.Response(400, "Validation failure or user/org not found");
            s.Response(404, "Organization not found");
        });
    }

    public override async Task HandleAsync(AddOrganizationMemberRequest req, CancellationToken ct)
    {
        string targetUserId = req.UserId ?? string.Empty;
        if (string.IsNullOrEmpty(targetUserId))
        {
            targetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        if (string.IsNullOrEmpty(targetUserId))
        {
            ThrowError("Could not resolve user to add.");
        }

        var command = new AddOrganizationMemberCommand
        {
            OrganizationId = req.OrganizationId,
            TargetUserId = targetUserId,
            Role = req.Role
        };

        try
        {
            var result = await mediator.Send(command, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "Organization not found.")
            {
                await Send.NotFoundAsync(ct);
            }
            else
            {
                ThrowError(ex.Message);
            }
        }
        catch (UnauthorizedAccessException)
        {
            await Send.UnauthorizedAsync(ct);
        }
    }
}
