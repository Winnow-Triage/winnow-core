using FastEndpoints;
using MediatR;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations.ValueObjects;

namespace Winnow.API.Features.Admin.Organizations.Create;

public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public Email ContactEmail { get; set; } = new Email("");
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
}

public class CreateOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public Guid DefaultProjectId { get; set; }
    public string DefaultProjectApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Admin endpoint to manually create a new organization/tenant.
/// </summary>
public sealed class CreateOrganizationEndpoint(IMediator mediator) : Endpoint<CreateOrganizationRequest, CreateOrganizationResponse>
{
    public override void Configure()
    {
        Post("/admin/organizations");
        Roles("SuperAdmin");
        Summary(s =>
        {
            s.Summary = "Create a new organization (SuperAdmin only)";
            s.Description = "Manually creates a new tenant organization in the system.";
            s.Response<CreateOrganizationResponse>(200, "Organization created successfully");
            s.Response(400, "Validation failure");
            s.Response(401, "Unauthorized");
            s.Response(403, "Forbidden");
        });
    }

    public override async Task HandleAsync(CreateOrganizationRequest req, CancellationToken ct)
    {
        var command = new CreateOrganizationCommand
        {
            Name = req.Name,
            ContactEmail = req.ContactEmail,
            Plan = req.Plan
        };

        var result = await mediator.Send(command, ct);
        await Send.OkAsync(result, ct);
    }
}
