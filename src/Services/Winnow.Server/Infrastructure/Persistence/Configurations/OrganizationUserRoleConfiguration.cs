using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Security;
using Winnow.Server.Infrastructure.Identity;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class OrganizationUserRoleConfiguration : IEntityTypeConfiguration<OrganizationUserRole>
{
    public void Configure(EntityTypeBuilder<OrganizationUserRole> builder)
    {
        builder.HasKey(our => new { our.OrganizationId, our.UserId, our.RoleId });

        builder.HasOne(our => our.Organization)
            .WithMany()
            .HasForeignKey(our => our.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(our => our.Role)
            .WithMany(r => r.OrganizationUserRoles)
            .HasForeignKey(our => our.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(our => our.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
