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
    WinnowDbContext dbContext,
    IApiKeyService apiKeyService) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var apiKeyValues))
        {
            return AuthenticateResult.NoResult();
        }

        var incomingKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(incomingKey))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Expected Format: wm_live_{ProjectId}_{RandomSecret}
            // Parts: 
            // 0: wm
            // 1: live
            // 2: ProjectId string (N format)
            // 3: RandomSecret string
            var parts = incomingKey.Split('_');

            // Check if it's formatted correctly and we can parse the ProjectId
            if (parts.Length < 4 || !Guid.TryParse(parts[2], out var targetProjectId))
            {
                return AuthenticateResult.Fail("Malformed API Key");
            }

            // Look up the project exclusively by ID (fast index scan)
            var project = await dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == targetProjectId);

            if (project == null)
            {
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Verify primary key
            bool isPrimaryValid = apiKeyService.VerifyKey(incomingKey, project.ApiKeyHash);

            // Verify secondary key if primary failed
            bool isSecondaryValid = false;
            if (!isPrimaryValid && !string.IsNullOrEmpty(project.SecondaryApiKeyHash))
            {
                // Check if secondary key is still valid (not expired)
                if (!project.SecondaryApiKeyExpiresAt.HasValue || project.SecondaryApiKeyExpiresAt.Value > DateTimeOffset.UtcNow)
                {
                    isSecondaryValid = apiKeyService.VerifyKey(incomingKey, project.SecondaryApiKeyHash);
                }
            }

            if (!isPrimaryValid && !isSecondaryValid)
            {
                // Return a generic error to prevent timing attacks / enumeration
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Create identity for the project
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, project.Id.ToString()),
                new Claim(ClaimTypes.Name, project.Name),
                new Claim("ProjectId", project.Id.ToString()),
                new Claim("organization", project.OrganizationId.ToString()),
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
