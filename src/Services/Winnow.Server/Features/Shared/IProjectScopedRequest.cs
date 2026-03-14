namespace Winnow.Server.Features.Shared;

public interface IProjectScopedRequest : IOrganizationScopedRequest
{
    Guid CurrentProjectId { get; set; }
}
