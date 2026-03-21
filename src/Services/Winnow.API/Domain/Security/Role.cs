using Winnow.API.Domain.Core;

namespace Winnow.API.Domain.Security;

public class Role : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Guid? OrganizationId { get; private set; }

    private readonly List<RolePermission> _permissions = [];
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private readonly List<Winnow.API.Domain.Organizations.OrganizationMember> _organizationMembers = [];
    public IReadOnlyCollection<Winnow.API.Domain.Organizations.OrganizationMember> OrganizationMembers => _organizationMembers.AsReadOnly();

    private Role()
    {
        Name = null!;
    }

    public Role(string name, Guid? organizationId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required.", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        OrganizationId = organizationId;
    }
}
