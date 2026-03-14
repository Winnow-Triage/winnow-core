using Winnow.Server.Features.Auth.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Auth.Login;

public record LoginCommand : IRequest<AuthResult>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public Guid? OrganizationId { get; init; }
    public string TenantId { get; init; } = "default";
}

public class LoginHandler(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    IConfiguration config,
    Winnow.Server.Infrastructure.Security.IApiKeyService apiKeyService) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new InvalidOperationException("Invalid credentials");
        }

        var validPass = await userManager.CheckPasswordAsync(user, request.Password);
        if (!validPass)
        {
            throw new InvalidOperationException("Invalid credentials");
        }

        var organizationMemberships = await dbContext.OrganizationMembers
            .Where(om => om.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (organizationMemberships.Count == 0)
        {
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

            await dbContext.SaveChangesAsync(cancellationToken);

            organizationMemberships = [organizationMember];
        }

        Guid selectedOrganizationId;

        if (request.OrganizationId.HasValue)
        {
            var membership = organizationMemberships.FirstOrDefault(om => om.OrganizationId == request.OrganizationId.Value);
            if (membership == null)
            {
                throw new InvalidOperationException("You do not have access to this organization.");
            }
            selectedOrganizationId = request.OrganizationId.Value;
        }
        else if (organizationMemberships.Count > 1)
        {
            var orgIds = organizationMemberships.Select(om => om.OrganizationId).ToList();
            var orgs = await dbContext.Organizations
                .Where(o => orgIds.Contains(o.Id))
                .Select(o => new OrganizationDto
                {
                    Id = o.Id,
                    Name = o.Name
                })
                .ToListAsync(cancellationToken);

            return new AuthResult
            {
                RequiresOrganizationSelection = true,
                Organizations = orgs,
                Email = user.Email ?? "",
                FullName = user.FullName,
                UserId = user.Id
            };
        }
        else
        {
            selectedOrganizationId = organizationMemberships.First().OrganizationId;
        }

        tenantContext.CurrentOrganizationId = selectedOrganizationId;

        var project = await dbContext.Projects
            .Where(p => p.OrganizationId == selectedOrganizationId)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
        {
            throw new InvalidOperationException("Your organization has no projects. Please contact an administrator.");
        }

        var jwtSettings = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "super_secret_key_at_least_32_chars_long_for_safety");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("email_verified", user.EmailConfirmed.ToString().ToLowerInvariant()),
            new("tenant_id", request.TenantId)
        };

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
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        return new AuthResult
        {
            Token = tokenString,
            UserId = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            IsEmailVerified = user.EmailConfirmed,
            DefaultProjectId = project.Id,
            ApiKey = "",
            ActiveOrganizationId = selectedOrganizationId
        };
    }
}
