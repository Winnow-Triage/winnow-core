using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Features.Admin.Users.Create;

public record AdminCreateUserCommand : IRequest<AdminCreateUserResponse>
{
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Role { get; init; } = "Member";
}

public class AdminCreateUserHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<AdminCreateUserHandler> logger) : IRequestHandler<AdminCreateUserCommand, AdminCreateUserResponse>
{
    public async Task<AdminCreateUserResponse> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            await userManager.AddToRoleAsync(user, request.Role);
        }

        logger.LogInformation("SuperAdmin created new user: {Email} with role {Role}", request.Email, request.Role);

        return new AdminCreateUserResponse
        {
            Id = user.Id,
            Message = "User account created successfully."
        };
    }
}
