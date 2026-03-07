using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Reports;
using Winnow.Server.Domain.Reports.ValueObjects;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.Status)
            .HasConversion(
                status => status.Name,
                dbString => ReportStatus.FromName(dbString)
            )
            .HasColumnName("Status")
            .IsRequired();

        builder.Property(r => r.ConfidenceScore)
            .HasConversion(
                cs => cs.HasValue ? cs.Value.Score : (double?)null,
                v => v.HasValue ? new ConfidenceScore(v.Value) : null
            )
            .HasColumnName("ConfidenceScore");

        builder.Property(r => r.SuggestedConfidenceScore)
            .HasConversion(
                cs => cs.HasValue ? cs.Value.Score : (double?)null,
                v => v.HasValue ? new ConfidenceScore(v.Value) : null
            )
            .HasColumnName("SuggestedConfidenceScore");

        builder.Ignore(r => r.DomainEvents);
    }
}
