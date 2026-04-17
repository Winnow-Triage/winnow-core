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
    IConfiguration config,
    JwtSettings jwtSettings,
    Winnow.API.Infrastructure.Security.IApiKeyService apiKeyService,
    IEmailService emailService,
    Winnow.API.Services.Discord.IInternalOpsNotifier internalOpsNotifier) : IRequestHandler<RegisterCommand, AuthResult>
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

        // Notify internal operations of new signup - MUST happen before SaveChangesAsync for Outbox to capture it
        try
        {
            await internalOpsNotifier.NotifyNewSignupAsync(request.Email);
        }
        catch (Exception ex)
        {
            // Do not block registration for notification failures
            Console.WriteLine($"[REGISTER] Internal notification failed: {ex.Message}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        tenantContext.CurrentOrganizationId = organization.Id;

        try
        {
            Console.WriteLine($"[REGISTER] Sending welcome email to {user.Email}");
            await emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
            Console.WriteLine($"[REGISTER] Welcome email sent to {user.Email}");

            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var appUrl = config["AppUrl"] ?? "https://app.winnowtriage.com";
            var verificationUrl = $"{appUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

            Console.WriteLine($"[REGISTER] Sending verification email to {user.Email}");
            await emailService.SendEmailVerificationAsync(user.Email!, new Uri(verificationUrl));
            Console.WriteLine($"[REGISTER] Verification email sent to {user.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[REGISTER] FAILED to send emails to {user.Email}: {ex.Message}");
        }

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
