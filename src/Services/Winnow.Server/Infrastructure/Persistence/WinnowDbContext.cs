using Microsoft.EntityFrameworkCore;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;

namespace Winnow.Server.Infrastructure.Persistence;

public class WinnowDbContext(DbContextOptions<WinnowDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Dynamic connection string based on tenant
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(tenantContext.ConnectionString);
            optionsBuilder.AddInterceptors(new SqliteVectorConnectionInterceptor());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite does not support DateTimeOffset or TimeZone storage natively.
        // It stores everything as strings. EF Core, by default, reads these back as "Unspecified".
        // When the server (running in local time) sees "Unspecified", it often treats it as Local.
        // This causes Shift: Stored 22:00 -> Read as 22:00 Local -> Converted to UTC as 04:00 (+6h).
        
        // This converter forces all DateTime properties to be treated as UTC immediately upon read.
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullDateTimeConverter);
                }
            }
        }
    }

    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<IntegrationConfig> IntegrationConfigs { get; set; } = null!;
}
