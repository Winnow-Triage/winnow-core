namespace Winnow.API.Domain.Security;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class RolePermission
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
