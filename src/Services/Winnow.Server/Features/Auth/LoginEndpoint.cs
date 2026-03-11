using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Auth;

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

public sealed class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    IConfiguration config,
    Winnow.Server.Infrastructure.Security.IApiKeyService apiKeyService) : Endpoint<LoginRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Log in to the application";
            s.Description = "Authenticates a user and returns a JWT token along with project details.";
            s.Response<AuthResponse>(200, "Login successful");
            s.Response(400, "Invalid credentials or request");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            ThrowError("Invalid credentials");
        }

        var validPass = await userManager.CheckPasswordAsync(user, req.Password);
        if (!validPass)
        {
            ThrowError("Invalid credentials");
        }

        // Get user's organization memberships
        var organizationMemberships = await dbContext.OrganizationMembers
            .Where(om => om.UserId == user.Id)
            .ToListAsync(ct);

        if (organizationMemberships.Count == 0)
        {
            // Create a default organization for the user if they don't have one
            var organization = new Domain.Organizations.Organization(
                $"{user.FullName}'s Organization",
                new Domain.Common.Email(user.Email!)
            );

            var organizationMember = new Winnow.Server.Domain.Organizations.OrganizationMember(
                organization.Id,
                user.Id,
                "owner");

            dbContext.Organizations.Add(organization);
            dbContext.OrganizationMembers.Add(organizationMember);

            // Create a Default Project for the new organization
            var projectId = Guid.NewGuid();
            var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
            var initialProject = new Domain.Projects.Project(
                organization.Id,
                "Default Project",
                user.Id,
                apiKeyService.HashKey(plaintextKey),
                projectId
            );
            dbContext.Projects.Add(initialProject);

            await dbContext.SaveChangesAsync(ct);

            organizationMemberships = [organizationMember];
        }

        Guid selectedOrganizationId;

        // Determine which organization to use
        if (req.OrganizationId.HasValue)
        {
            // Verify user belongs to the requested organization
            var membership = organizationMemberships.FirstOrDefault(om => om.OrganizationId == req.OrganizationId.Value);
            if (membership == null)
            {
                ThrowError("You do not have access to this organization.");
            }
            selectedOrganizationId = req.OrganizationId.Value;
        }
        else if (organizationMemberships.Count > 1)
        {
            // User belongs to multiple organizations and hasn't selected one yet
            var orgIds = organizationMemberships.Select(om => om.OrganizationId).ToList();
            var orgs = await dbContext.Organizations
                .Where(o => orgIds.Contains(o.Id))
                .Select(o => new OrganizationDto
                {
                    Id = o.Id,
                    Name = o.Name
                })
                .ToListAsync(ct);

            await Send.OkAsync(new AuthResponse
            {
                RequiresOrganizationSelection = true,
                Organizations = orgs,
                Email = user.Email ?? "",
                FullName = user.FullName,
                UserId = user.Id
            });
            return;
        }
        else
        {
            // User belongs to exactly one organization
            selectedOrganizationId = organizationMemberships.First().OrganizationId;
        }

        tenantContext.CurrentOrganizationId = selectedOrganizationId;

        // Get default project (first one in this organization)
        var project = await dbContext.Projects
            .Where(p => p.OrganizationId == selectedOrganizationId)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (project == null)
        {
            ThrowError("Your organization has no projects. Please contact an administrator.");
        }

        var token = await GenerateJwtAsync(user);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        HttpContext.Response.Cookies.Append("winnow_auth", token, cookieOptions);

        await Send.OkAsync(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            IsEmailVerified = user.EmailConfirmed,
            DefaultProjectId = project.Id,
            ApiKey = "", // Cannot return hashed key
            ActiveOrganizationId = selectedOrganizationId
        });
    }

    private async Task<string> GenerateJwtAsync(ApplicationUser user)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("email_verified", user.EmailConfirmed.ToString().ToLowerInvariant()),
            new("tenant_id", tenantContext.TenantId ?? "default")
        };


        // Add organization ID claim if user has one
        if (tenantContext.CurrentOrganizationId.HasValue)
        {
            claims.Add(new Claim("organization", tenantContext.CurrentOrganizationId.Value.ToString()));
        }

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
