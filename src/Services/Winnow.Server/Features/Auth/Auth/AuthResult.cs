namespace Winnow.Server.Features.Auth.Auth;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public record AuthResult
{
    public string Token { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public Guid DefaultProjectId { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public bool RequiresOrganizationSelection { get; init; }
    public List<OrganizationDto> Organizations { get; init; } = [];
    public Guid ActiveOrganizationId { get; init; }
    public bool IsEmailVerified { get; init; }
}
