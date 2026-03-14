using System.Security.Claims;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Winnow.Server.Features.Admin.Users.Impersonate;

public class ImpersonateUserRequest
{
    public string Id { get; set; } = string.Empty;
}

public class ImpersonateUserResponse
{
    public string TargetUserEmail { get; set; } = string.Empty;
}

/// <summary>
/// Allows a SuperAdmin to generate a valid JWT for any user account.
/// THIS IS A HIGHLY SENSITIVE SECURITY FEATURE.
/// </summary>
public sealed class ImpersonateUserEndpoint(IMediator mediator) : Endpoint<ImpersonateUserRequest, ImpersonateUserResponse>
{
    public override void Configure()
    {
        Post("/admin/users/{id}/impersonate");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Impersonate a user (SuperAdmin only)";
            s.Description = "Generates a session token (JWT) for the target user, allowing an admin to see the app from their perspective.";
            s.Response<ImpersonateUserResponse>(200, "Impersonation token generated");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(ImpersonateUserRequest req, CancellationToken ct)
    {
        var command = new ImpersonateUserCommand
        {
            TargetUserId = req.Id,
            AdminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown",
            AdminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown"
        };

        try
        {
            var result = await mediator.Send(command, ct);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            };
            HttpContext.Response.Cookies.Append("winnow_auth", result.TokenString, cookieOptions);

            var response = new ImpersonateUserResponse
            {
                TargetUserEmail = result.TargetUserEmail
            };

            await Send.OkAsync(response, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

