using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.API.Features.Auth.Get;

public class UserMeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public Guid? ActiveOrganizationId { get; set; }
    public Guid? DefaultProjectId { get; set; }
}

[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("api")]
public sealed class GetMeEndpoint(IMediator mediator) : EndpointWithoutRequest<UserMeResponse>
{
    public override void Configure()
    {
        Get("/auth/me");

        // Ensure standard .NET rate limiting policies are applied
        Options(x => x.RequireRateLimiting("api"));
        Summary(s =>
        {
            s.Summary = "Get current user information";
            s.Description = "Returns details about the currently authenticated user based on the session cookie.";
            s.Response<UserMeResponse>(200, "User details retrieved successfully");
            s.Response(401, "Not authenticated");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orgIdClaim = User.FindFirstValue("organization");

        var query = new GetMeQuery(userId, orgIdClaim);
        var result = await mediator.Send(query, ct);

        if (!result.IsSuccess)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(new UserMeResponse
        {
            Id = result.Id,
            Email = result.Email,
            FullName = result.FullName,
            IsEmailVerified = result.IsEmailVerified,
            Roles = result.Roles,
            Permissions = result.Permissions,
            ActiveOrganizationId = result.ActiveOrganizationId,
            DefaultProjectId = result.DefaultProjectId
        }, ct);
    }
}
