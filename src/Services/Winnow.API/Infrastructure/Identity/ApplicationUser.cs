using Microsoft.AspNetCore.Identity;

namespace Winnow.API.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationship: A user can be a member/owner of multiple projects
    public ICollection<Winnow.API.Domain.Projects.Project> Projects { get; } = new List<Winnow.API.Domain.Projects.Project>();

    // Relationship: A user can be a member of multiple organizations
    public ICollection<Winnow.API.Domain.Organizations.OrganizationMember> OrganizationMemberships { get; set; } = new List<Winnow.API.Domain.Organizations.OrganizationMember>();
}
