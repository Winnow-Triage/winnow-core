using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Infrastructure.Security;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-Winnow-Key";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    WinnowDbContext dbContext) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKeyValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var project = await dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApiKey == apiKey);

            if (project == null)
            {
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Create identity for the project
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, project.Id.ToString()),
                new Claim(ClaimTypes.Name, project.Name),
                new Claim("ProjectId", project.Id.ToString()),
                new Claim(ClaimTypes.Role, "Project")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating API key");
            return AuthenticateResult.Fail(ex);
        }
    }
}
