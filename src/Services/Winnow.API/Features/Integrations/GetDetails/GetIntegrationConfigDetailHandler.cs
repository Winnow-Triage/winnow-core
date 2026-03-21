using System.Text.Json;
using MediatR;
using Winnow.API.Infrastructure.Security.Authorization;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Features.Shared;

namespace Winnow.API.Features.Integrations.GetDetails;

[RequirePermission("integrations:read")]
public record GetIntegrationConfigDetailQuery(Guid Id, Guid CurrentOrganizationId) : IRequest<GetIntegrationConfigDetailResult>, IOrgScopedRequest;

public record GetIntegrationConfigDetailResult(bool IsSuccess, IntegrationDetailResponse? Data = null, string? ErrorMessage = null, int? StatusCode = null);

public class GetIntegrationConfigDetailHandler(WinnowDbContext db) : IRequestHandler<GetIntegrationConfigDetailQuery, GetIntegrationConfigDetailResult>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<GetIntegrationConfigDetailResult> Handle(GetIntegrationConfigDetailQuery request, CancellationToken cancellationToken)
    {
        var integration = await db.Integrations.FindAsync([request.Id], cancellationToken);

        if (integration == null)
        {
            return new GetIntegrationConfigDetailResult(false, null, "Integration not found", 404);
        }

        // Serialize Config to JSON (secrets will be masked by the domain model)
        var maskedJson = JsonSerializer.Serialize(integration.Config, _jsonOptions);

        var response = new IntegrationDetailResponse
        {
            Id = integration.Id,
            Provider = integration.Provider,
            SettingsJson = maskedJson,
            IsActive = integration.IsActive
        };

        return new GetIntegrationConfigDetailResult(true, response);
    }
}
