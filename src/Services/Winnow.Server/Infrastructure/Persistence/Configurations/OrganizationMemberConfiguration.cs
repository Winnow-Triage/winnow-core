using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Infrastructure.Identity;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("OrganizationMembers");

        builder.HasKey(om => om.Id);

        builder.HasIndex(om => new { om.OrganizationId, om.UserId })
            .IsUnique();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(om => om.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.OrganizationMemberships)
            .HasForeignKey(om => om.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(om => om.Role)
            .WithMany(r => r.OrganizationMembers)
            .HasForeignKey(om => om.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(om => om.DomainEvents);
    }
}
