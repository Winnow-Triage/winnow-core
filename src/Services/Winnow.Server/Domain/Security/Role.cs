using Winnow.Server.Domain.Core;

namespace Winnow.Server.Domain.Security;

public class Role : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private readonly List<OrganizationUserRole> _organizationUserRoles = [];
    public IReadOnlyCollection<OrganizationUserRole> OrganizationUserRoles => _organizationUserRoles.AsReadOnly();

    private Role()
    {
        Name = null!;
    }

    public Role(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required.", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
    }
}
