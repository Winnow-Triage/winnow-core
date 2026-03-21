namespace Winnow.API.Features.Shared;

public interface IProjectScopedRequest : IOrganizationScopedRequest
{
    Guid CurrentProjectId { get; set; }
}
