using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Security;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.Permissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.Roles)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
