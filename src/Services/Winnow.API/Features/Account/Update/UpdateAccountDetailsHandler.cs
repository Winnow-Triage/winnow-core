using Winnow.API.Features.Account.GetDetails;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Services.Emails;

namespace Winnow.API.Features.Account.Update;

public record UpdateAccountDetailsCommand : IRequest<AccountDetailsResponse>
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string CurrentUserId { get; init; } = string.Empty;
}

public class UpdateAccountDetailsHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService) : IRequestHandler<UpdateAccountDetailsCommand, AccountDetailsResponse>
{
    public async Task<AccountDetailsResponse> Handle(UpdateAccountDetailsCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.CurrentUserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
            if (existingUser != null && existingUser.Id != user.Id)
            {
                throw new InvalidOperationException("Email is already in use by another account.");
            }

            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim(); // Sync Email and UserName
            user.EmailConfirmed = false;

            // Generate and send new verification token
            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var verificationUrl = $"http://localhost:5173/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

            try
            {
                await emailService.SendEmailVerificationAsync(user.Email, new Uri(verificationUrl));
            }
            catch (Exception ex)
            {
                // Log but don't fail the update
                Console.WriteLine($"[ACCOUNT] Failed to send verification email for {user.Email}: {ex.Message}");
            }
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Errors.First().Description);
        }

        return new AccountDetailsResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        };
    }
}
