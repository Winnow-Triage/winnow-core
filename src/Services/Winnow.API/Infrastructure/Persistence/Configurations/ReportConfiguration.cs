using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Reports;
using Winnow.API.Domain.Reports.ValueObjects;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

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

        builder.Property(r => r.Assets)
            .HasField("_assetIds")
            .HasColumnName("Assets")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v) ? new List<Guid>() : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>()
            )
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyCollection<Guid>>(
                (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == null && c2 == null,
                c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                c => c != null ? c.ToList() : new List<Guid>()));
    }
}
