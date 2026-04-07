using MediatR;
using Microsoft.AspNetCore.Identity;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Features.Account.GetDetails;

public record GetAccountDetailsQuery : IRequest<AccountDetailsResponse>
{
    public string CurrentUserId { get; init; } = string.Empty;
}

public class GetAccountDetailsHandler(UserManager<ApplicationUser> userManager) : IRequestHandler<GetAccountDetailsQuery, AccountDetailsResponse>
{
    public async Task<AccountDetailsResponse> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.CurrentUserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        return new AccountDetailsResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            EmailBounced = user.EmailBounced
        };
    }
}
