using MediatR;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Organizations.ValueObjects;
using Winnow.API.Domain.Projects;
using Winnow.API.Infrastructure.Persistence;
using Winnow.API.Infrastructure.Security;

namespace Winnow.API.Features.Admin.Organizations.Create;

public record CreateOrganizationCommand : IRequest<CreateOrganizationResponse>
{
    public string Name { get; init; } = string.Empty;
    public Email ContactEmail { get; init; } = new Email("");
    public SubscriptionPlan Plan { get; init; } = SubscriptionPlan.Free;
}

public class CreateOrganizationHandler(
    WinnowDbContext dbContext,
    IApiKeyService apiKeyService) : IRequestHandler<CreateOrganizationCommand, CreateOrganizationResponse>
{
    public async Task<CreateOrganizationResponse> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = new Organization
        (
            request.Name,
            request.ContactEmail,
            request.Plan
        );

        dbContext.Organizations.Add(organization);

        // Create a Default Project for the new organization
        var projectId = Guid.NewGuid();
        var plaintextKey = apiKeyService.GeneratePlaintextKey(projectId);
        var project = new Project
        (
            organization.Id,
            "Default Project",
            "", // Admin created orgs might not have an owner yet
            apiKeyService.HashKey(plaintextKey)
        );
        dbContext.Projects.Add(project);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            SubscriptionTier = organization.Plan.Name,
            DefaultProjectId = project.Id,
            DefaultProjectApiKey = plaintextKey
        };
    }
}
