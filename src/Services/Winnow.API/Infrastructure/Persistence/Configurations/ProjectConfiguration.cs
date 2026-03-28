using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Projects;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(p => p.ProjectMembers)
            .WithOne()
            .HasForeignKey(m => m.ProjectId);
        builder.Navigation(p => p.ProjectMembers).HasField("_members");

        builder.Ignore(p => p.Integrations);
        builder.Ignore(p => p.Clusters);
        builder.Ignore(p => p.Reports);
        builder.Ignore(p => p.DomainEvents);
    }
}
