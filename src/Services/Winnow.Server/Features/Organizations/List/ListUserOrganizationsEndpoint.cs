using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Organizations.List;

/// <summary>
/// Organization data transfer object.
/// </summary>
public class OrganizationDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the organization.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the organization.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}


public sealed class ListUserOrganizationsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<List<OrganizationDto>>
{
    public override void Configure()
    {
        Get("/organizations");
        Summary(s =>
        {
            s.Summary = "List all organizations the current user belongs to";
            s.Description = "Returns a list of all organizations where the authenticated user is a member.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            ThrowError("User not authenticated.");
            return;
        }

        var query = new ListUserOrganizationsQuery(userId);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.ErrorMessage ?? "Internal Server Error", result.StatusCode ?? 500);
            return;
        }

        await Send.OkAsync(result.Data!, ct);
    }
}
