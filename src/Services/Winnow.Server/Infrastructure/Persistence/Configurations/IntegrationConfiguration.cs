using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Integrations.Domain;
using Winnow.Server.Domain.Integrations;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

public class IntegrationConfiguration(EncryptedStringConverter encryptedConverter) : IEntityTypeConfiguration<Integration>
{
    private readonly EncryptedStringConverter _encryptedConverter = encryptedConverter;

    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Token)
            .HasConversion(_encryptedConverter);

        // Map polymorphic JSON config
        var jsonOptions = new JsonSerializerOptions { };

        builder.Property(i => i.Config)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<IntegrationConfig>(v, jsonOptions)!
            );

        builder.Ignore(i => i.DomainEvents);
    }
}
