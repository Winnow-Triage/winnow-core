using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Clusters;
using Winnow.Server.Domain.Clusters.ValueObjects;
using Winnow.Server.Domain.Common;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class ClusterConfiguration : IEntityTypeConfiguration<Cluster>
{
    public void Configure(EntityTypeBuilder<Cluster> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(c => c.Status)
            .HasConversion(
                status => status.Name,
                dbString => ClusterStatus.FromName(dbString))
            .HasColumnName("Status")
            .IsRequired();

        builder.Property(c => c.ReportIds)
            .HasField("_reportIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(c => c.SuggestedMergeConfidenceScore)
            .HasConversion(
                cs => cs.HasValue ? cs.Value.Score : (double?)null,
                v => v.HasValue ? new ConfidenceScore(v.Value) : null)
            .HasColumnName("SuggestedMergeConfidenceScore");

        // (Optional but recommended) Explicitly map the Centroid to ensure it uses Postgres vector/arrays
        builder.Property(c => c.Centroid)
            .HasColumnName("Centroid");

        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.ReportCount);
    }
}