using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Projects;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");

        builder.HasKey(pm => pm.Id);

        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map to ApplicationUser (Identity)
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(pm => pm.DomainEvents);
    }
}
