using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Security;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class Permission : IAggregateRoot
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    private readonly List<RolePermission> _roles = [];
    public IReadOnlyCollection<RolePermission> Roles => _roles.AsReadOnly();

    private Permission()
    {
        Name = null!;
    }

    public Permission(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name is required.", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
    }
}
