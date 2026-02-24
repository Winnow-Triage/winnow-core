using FastEndpoints;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.Persistence;

namespace Winnow.Server.Features.Admin;

public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Free";
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
public sealed class CreateOrganizationEndpoint(
    WinnowDbContext dbContext,
    Winnow.Server.Infrastructure.Security.IApiKeyService apiKeyService) : Endpoint<CreateOrganizationRequest, CreateOrganizationResponse>
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
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            SubscriptionTier = req.SubscriptionTier,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Organizations.Add(organization);

        // Create a Default Project for the new organization
        var projectId = Guid.NewGuid();
        var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Project
        {
            Id = projectId,
            Name = "Default Project",
            OwnerId = "", // Admin created orgs might not have an owner yet
            OrganizationId = organization.Id,
            ApiKeyHash = apiKeyService.HashKey(plaintextKey)
        };
        dbContext.Projects.Add(project);

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new CreateOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.SubscriptionTier,
            DefaultProjectId = project.Id,
            DefaultProjectApiKey = plaintextKey
        }, ct);
    }
}
