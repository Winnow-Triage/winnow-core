using System;

namespace Winnow.Server.Entities;

public class OrganizationInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    public List<Guid> InitialTeamIds { get; set; } = [];
    public List<Guid> InitialProjectIds { get; set; } = [];

    // Navigation property
    public Organization Organization { get; set; } = null!;
}
