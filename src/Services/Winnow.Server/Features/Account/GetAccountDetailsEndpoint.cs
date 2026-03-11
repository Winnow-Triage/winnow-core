using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Winnow.Server.Infrastructure.Identity;
using Winnow.Server.Domain.Reports.ValueObjects;
using Winnow.Server.Domain.Clusters.ValueObjects;

namespace Winnow.Server.Features.Account;

public class AccountDetailsResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class GetAccountDetailsEndpoint(UserManager<ApplicationUser> userManager)
    : EndpointWithoutRequest<AccountDetailsResponse>
{
    public override void Configure()
    {
        Get("/account/me");
        Summary(s =>
        {
            s.Summary = "Get current user account details";
            s.Description = "Returns the profile information for the currently authenticated user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new AccountDetailsResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName
        }, ct);
    }
}
