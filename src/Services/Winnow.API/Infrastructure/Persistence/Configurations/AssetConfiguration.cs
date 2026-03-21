using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Assets;
using Winnow.API.Domain.Assets.ValueObjects;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.S3Key)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Status)
            .HasConversion(
                a => a.Value,
                a => AssetStatus.FromName(a))
            .IsRequired();

        builder.Ignore(a => a.DomainEvents);
    }
}
