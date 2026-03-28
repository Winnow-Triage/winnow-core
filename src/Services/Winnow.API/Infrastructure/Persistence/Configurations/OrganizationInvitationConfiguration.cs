using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Organizations;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

public class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { };

    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.ToTable("OrganizationInvitations");

        builder.HasKey(oi => oi.Id);

        builder.HasIndex(oi => oi.Token)
            .IsUnique();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(oi => oi.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(oi => oi.Email)
            .HasConversion(
                email => email.Value,
                value => new Winnow.API.Domain.Common.Email(value)
            )
            .IsRequired()
            .HasMaxLength(255);

        var listComparer = new ValueComparer<IReadOnlyCollection<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(oi => oi.InitialTeamIds)
            .HasField("_initialTeamIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, _jsonOptions) ?? new List<Guid>(),
                listComparer
            );

        builder.Property(oi => oi.InitialProjectIds)
            .HasField("_initialProjectIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<Guid>>(v, _jsonOptions) ?? new List<Guid>(),
                listComparer
            );

        builder.Ignore(oi => oi.DomainEvents);
    }
}
