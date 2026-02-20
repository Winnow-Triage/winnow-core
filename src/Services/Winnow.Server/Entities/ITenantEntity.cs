namespace Winnow.Server.Entities;

/// <summary>
/// Interface for entities that belong to an organization in a multi-tenant system.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The organization that owns this entity.
    /// </summary>
    Guid OrganizationId { get; set; }
}