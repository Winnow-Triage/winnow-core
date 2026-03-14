using System.Security.Claims;
using FastEndpoints;
using MediatR;

namespace Winnow.Server.Features.Account.GetDetails;

public class AccountDetailsResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class GetAccountDetailsEndpoint(IMediator mediator)
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

        var query = new GetAccountDetailsQuery
        {
            CurrentUserId = userId
        };

        try
        {
            var result = await mediator.Send(query, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
