using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Winnow.Server.Domain.Services;
using Winnow.Server.Entities;
using Winnow.Server.Infrastructure.MultiTenancy;
using Winnow.Server.Infrastructure.Security;

namespace Winnow.Server.Infrastructure.Persistence;

#pragma warning disable CS9113 // Parameter is unused
public class WinnowDbContext(DbContextOptions<WinnowDbContext> options, ITenantContext tenantContext, IConfiguration configuration) : IdentityDbContext<ApplicationUser>(options)
#pragma warning restore CS9113 // Parameter is unused
{
    // Note: tenantContext is intentionally not used in OnModelCreating to avoid model drift
    // when migrations are created with different tenant context values than what's used at runtime.
    // Query filters are applied at runtime via SaveChanges interceptors.

    private readonly string _encryptionKey = configuration["Encryption:MasterKey"]
        ?? throw new InvalidOperationException("Encryption config: 'Encryption:MasterKey' is missing.");

    private static readonly JsonSerializerOptions _jsonOptions = new() { };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure database-specific features
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<Report>()
                .Property(b => b.Embedding)
                .HasColumnType("vector(384)")
                .HasConversion(
                    v => v == null ? null : new Pgvector.Vector(v),
                    v => v == null ? null : v.ToArray()
                );
            modelBuilder.Entity<Cluster>()
                .Property(b => b.Centroid)
                .HasColumnType("vector(384)")
                .HasConversion(
                    v => v == null ? null : new Pgvector.Vector(v),
                    v => v == null ? null : v.ToArray()
                );
        }
        else if (Database.IsSqlite())
        {
            // SQLite does not support DateTimeOffset or TimeZone storage natively.
            // It stores everything as strings. EF Core, by default, reads these back as "Unspecified".
            // When the server (running in local time) sees "Unspecified", it often treats it as Local.
            // This causes Shift: Stored 22:00 -> Read as 22:00 Local -> Converted to UTC as 04:00 (+6h).
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

            // Vector conversion for SQLite
            modelBuilder.Entity<Report>()
                .Property(b => b.Embedding)
                .HasConversion(
                    v => v == null ? null : VectorCalculator.FloatsToBytes(v),
                    v => v == null ? null : VectorCalculator.BytesToFloats(v));
            modelBuilder.Entity<Cluster>()
                .Property(b => b.Centroid)
                .HasConversion(
                    v => v == null ? null : VectorCalculator.FloatsToBytes(v),
                    v => v == null ? null : VectorCalculator.BytesToFloats(v));
        }

        // Organization -> Team -> Project hierarchy
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.HasMany(o => o.Teams)
                .WithOne(t => t.Organization)
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Members)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasOne(t => t.Organization)
                .WithMany(o => o.Teams)
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Projects)
                .WithOne(p => p.Team)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<TeamMember>()
                .WithOne(tm => tm.Team)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(tm => tm.Id);

            entity.HasIndex(tm => new { tm.TeamId, tm.UserId })
                .IsUnique();

            entity.HasOne(tm => tm.Team)
                .WithMany()
                .HasForeignKey(tm => tm.TeamId);

            entity.HasOne(tm => tm.User)
                .WithMany()
                .HasForeignKey(tm => tm.UserId);
        });

        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasKey(om => om.Id);

            entity.HasIndex(om => new { om.UserId, om.OrganizationId })
                .IsUnique();

            entity.HasOne(om => om.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(om => om.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(om => om.User)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(om => om.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Project relationships
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(pm => pm.Id);

            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            entity.HasOne(pm => pm.Project)
                .WithMany()
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.HasOne(p => p.Team)
                .WithMany(t => t.Projects)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Organization)
                .WithMany(o => o.Projects)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Owner)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Report relationships
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Cluster)
                .WithMany(c => c.Reports)
                .HasForeignKey(r => r.ClusterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Cluster relationships
        modelBuilder.Entity<Cluster>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.Project)
                .WithMany()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.ProjectId);
        });

        // Asset -> Report relationship
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Report)
                .WithMany(r => r.Assets)
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Store enum as string — works for both SQLite and Postgres
            entity.Property(a => a.Status)
                .HasConversion<string>();
        });

        // Integration -> IntegrationConfig (polymorphic) configuration
        var encryptedConverter = new EncryptedStringConverter(_encryptionKey);

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.HasOne(i => i.Project)
                .WithMany(p => p.Integrations)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(i => i.Config)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Winnow.Integrations.Domain.IntegrationConfig>(v, _jsonOptions)!
                );

            entity.Property(e => e.Token)
                  .HasConversion(encryptedConverter);
        });

        // Organization -> Invitation relationship
        modelBuilder.Entity<OrganizationInvitation>(entity =>
        {
            entity.HasKey(oi => oi.Id);

            entity.HasIndex(oi => oi.Token)
                .IsUnique();

            entity.HasOne(oi => oi.Organization)
                .WithMany()
                .HasForeignKey(oi => oi.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(oi => oi.InitialTeamIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, _jsonOptions) ?? new List<Guid>()
                );

            entity.Property(oi => oi.InitialProjectIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, _jsonOptions) ?? new List<Guid>()
                );
        });

        // Note: Global query filters for tenant isolation are applied at runtime
        // via the ITenantContext service, not at model creation time.
        // This avoids model drift issues when migrations are created with different
        // tenant context values than what's used at runtime.
        // The tenantContext.CurrentOrganizationId is used in the runtime context
        // to apply filters dynamically.
    }

    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<TeamMember> TeamMembers { get; set; } = null!;
    public DbSet<OrganizationMember> OrganizationMembers { get; set; } = null!;
    public DbSet<Report> Reports { get; set; } = null!;
    public DbSet<Cluster> Clusters { get; set; } = null!;
    public DbSet<Asset> Assets { get; set; } = null!;
    public DbSet<Integration> Integrations { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<OrganizationInvitation> OrganizationInvitations { get; set; } = null!;
}
