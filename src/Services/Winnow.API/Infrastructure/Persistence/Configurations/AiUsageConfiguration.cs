using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Ai;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

public class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage>
{
    public void Configure(EntityTypeBuilder<AiUsage> builder)
    {
        builder.ToTable("AiUsages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Context).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ModelId).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
