using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Teams;
using Winnow.Server.Infrastructure.Identity;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");

        builder.HasKey(tm => tm.Id);

        builder.HasIndex(tm => new { tm.TeamId, tm.UserId })
            .IsUnique();

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map to ApplicationUser (Identity)
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(tm => tm.DomainEvents);
    }
}
