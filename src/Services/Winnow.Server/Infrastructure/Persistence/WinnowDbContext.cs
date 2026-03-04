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

            modelBuilder.Entity<Report>(entity =>
            {
                entity.Property(b => b.Embedding)
                    .HasColumnType("vector(384)")
                    .HasConversion(
                        v => v == null ? null : new Pgvector.Vector(v),
                        v => v == null ? null : v.ToArray()
                    );

                // HNSW Index for vector similarity search
                entity.HasIndex(r => r.Embedding)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");

                // Multi-tenancy index
                entity.HasIndex(r => r.OrganizationId);

                // Dashboard performance (Project + Status + Date DESC)
                entity.HasIndex(r => new { r.ProjectId, r.Status, r.CreatedAt })
                    .IsDescending(false, false, true);
            });

            modelBuilder.Entity<Cluster>(entity =>
            {
                entity.Property(b => b.Centroid)
                    .HasColumnType("vector(384)")
                    .HasConversion(
                        v => v == null ? null : new Pgvector.Vector(v),
                        v => v == null ? null : v.ToArray()
                    );

                // HNSW Index for centroid similarity (merging/dedup)
                entity.HasIndex(c => c.Centroid)
                    .HasMethod("hnsw")
                    .HasOperators("vector_cosine_ops");

                // Multi-tenancy index
                entity.HasIndex(c => c.OrganizationId);

                // Dashboard performance (Project + Status + Date DESC)
                entity.HasIndex(c => new { c.ProjectId, c.Status, c.CreatedAt })
                    .IsDescending(false, false, true);
            });
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

            entity.HasMany(o => o.Projects)
                .WithOne(p => p.Organization)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasOne(t => t.Organization)
                .WithMany(o => o.Teams)
                .HasForeignKey(t => t.OrganizationId)
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
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Organization)
                .WithMany(o => o.Projects)
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Owner)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(p => p.Integrations)
                .WithOne(i => i.Project)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Report relationships
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasOne(r => r.Project)
                .WithMany(p => p.Reports)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .WithMany(p => p.Clusters)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
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

            // Multi-tenancy index
            entity.HasIndex(a => a.OrganizationId);
        });

        // Integration -> IntegrationConfig (polymorphic) configuration
        var encryptedConverter = new EncryptedStringConverter(_encryptionKey);

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Token)
                .HasConversion(encryptedConverter);

            entity.Property(i => i.Config)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _jsonOptions),
                    v => JsonSerializer.Deserialize<Winnow.Integrations.Domain.IntegrationConfig>(v, _jsonOptions)!
                );

            // Multi-tenancy index
            entity.HasIndex(i => i.OrganizationId);
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
