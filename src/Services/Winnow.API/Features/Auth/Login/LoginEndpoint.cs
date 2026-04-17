using Winnow.API.Features.Auth.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Domain.Reports.ValueObjects;
using Winnow.API.Domain.Clusters.ValueObjects;
using Winnow.API.Infrastructure.Persistence;

namespace Winnow.API.Features.Auth.Login;

/// <summary>
/// Credentials for logging in.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional organization ID to log in under.
    /// </summary>
    public Guid? OrganizationId { get; set; }
}


[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("strict")]
public sealed class LoginEndpoint(
    IMediator mediator,
    Winnow.API.Infrastructure.MultiTenancy.ITenantContext tenantContext) : Endpoint<LoginRequest, AuthResult>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();

        // Ensure standard .NET rate limiting policies are applied
        Options(x => x.RequireRateLimiting("strict"));
        Summary(s =>
        {
            s.Summary = "Log in to the application";
            s.Description = "Authenticates a user and returns a JWT token along with project details.";
            s.Response<AuthResult>(200, "Login successful");
            s.Response(400, "Invalid credentials or request");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var command = new LoginCommand
        {
            Email = req.Email,
            Password = req.Password,
            OrganizationId = req.OrganizationId,
            TenantId = tenantContext.TenantId ?? "default"
        };

        try
        {
            var result = await mediator.Send(command, ct);

            if (!string.IsNullOrEmpty(result.Token))
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                };
                HttpContext.Response.Cookies.Append("winnow_auth", result.Token, cookieOptions);
            }

            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message);
        }
    }
}
