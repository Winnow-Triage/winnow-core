using Winnow.API.Features.Auth.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Services.Emails;

namespace Winnow.API.Features.Auth.Register;

public record RegisterCommand : IRequest<AuthResult>
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    WinnowDbContext dbContext,
    Winnow.API.Infrastructure.MultiTenancy.ITenantContext tenantContext,
    JwtSettings jwtSettings,
    Winnow.API.Infrastructure.Security.IApiKeyService apiKeyService,
    IPublisher publisher) : IRequestHandler<RegisterCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[REGISTER] Handle started for email: {request.Email}");
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Errors.First().Description);
        }

        var ownerRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Owner" && r.OrganizationId == null, cancellationToken);
        if (ownerRole == null)
        {
            throw new InvalidOperationException("System configuration error: 'Owner' role not found.");
        }

        var organization = new Domain.Organizations.Organization(
            $"{request.FullName}'s Organization",
            new Domain.Common.Email(request.Email)
        );

        var organizationMember = new Winnow.API.Domain.Organizations.OrganizationMember(
            organization.Id,
            user.Id,
            ownerRole.Id
        );

        dbContext.Organizations.Add(organization);
        dbContext.OrganizationMembers.Add(organizationMember);

        var projectId = Guid.NewGuid();
        var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Domain.Projects.Project(
            organization.Id,
            "Default Project",
            user.Id,
            apiKeyService.HashKey(plaintextKey),
            projectId
        );

        dbContext.Projects.Add(project);

        // Dispatch domain event for side effects (e.g. notifications, emails)
        await publisher.Publish(new UserRegisteredEvent(user), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        tenantContext.CurrentOrganizationId = organization.Id;

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("email_verified", user.EmailConfirmed.ToString().ToLowerInvariant())
        };

        if (tenantContext.CurrentOrganizationId.HasValue)
        {
            claims.Add(new Claim("organization", tenantContext.CurrentOrganizationId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings.Issuer,
            Audience = jwtSettings.Audience
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
            ApiKey = plaintextKey,
            ActiveOrganizationId = organization.Id
        };
    }
}
