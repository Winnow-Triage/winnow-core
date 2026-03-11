using Microsoft.AspNetCore.Identity;

namespace Winnow.Server.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationship: A user can be a member/owner of multiple projects
    public ICollection<Winnow.Server.Domain.Projects.Project> Projects { get; } = new List<Winnow.Server.Domain.Projects.Project>();

    // Relationship: A user can be a member of multiple organizations
    public ICollection<Winnow.Server.Domain.Organizations.OrganizationMember> OrganizationMemberships { get; set; } = new List<Winnow.Server.Domain.Organizations.OrganizationMember>();
}
