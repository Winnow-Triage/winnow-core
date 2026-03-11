using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Teams;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(t => t.TeamMembers)
            .WithOne()
            .HasForeignKey(m => m.TeamId);
        builder.Navigation(t => t.TeamMembers).HasField("_members");

        builder.Ignore(t => t.Projects);

        // Ignore DomainEvents since they aren't mapped to the database
        builder.Ignore(t => t.DomainEvents);
    }
}
