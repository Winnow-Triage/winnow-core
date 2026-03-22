using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using MassTransit;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Security;

namespace Winnow.API.Infrastructure.Persistence;

#pragma warning disable CS9113 // Parameter is unused
public class WinnowDbContext(DbContextOptions<WinnowDbContext> options, ITenantContext tenantContext, IConfiguration configuration) : IdentityDbContext<ApplicationUser>(options)
#pragma warning restore CS9113 // Parameter is unused
{
    // Note: tenantContext is intentionally not used in OnModelCreating to avoid model drift
    // when migrations are created with different tenant context values than what's used at runtime.
    // Query filters are applied at runtime via SaveChanges interceptors.

    private readonly string _encryptionKey = configuration["Encryption:MasterKey"]
        ?? throw new InvalidOperationException("Encryption config: 'Encryption:MasterKey' is missing.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var floatArrayComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<float[]>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == null && c2 == null,
            c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
            c => c != null ? c.ToArray() : null!);

        var guidListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
            (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == null && c2 == null,
            c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
            c => c != null ? c.ToList() : null!);

        // Configure database-specific features
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<Domain.Reports.Report>(entity =>
            {
                entity.Property(b => b.Embedding)
                    .HasColumnType("vector(384)")
                    .HasConversion(
                        v => v == null ? null : new Vector(v),
                        v => v == null ? null : v.ToArray()
                    )
                    .Metadata.SetValueComparer(floatArrayComparer);

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

            modelBuilder.Entity<Domain.Clusters.Cluster>(entity =>
            {
                entity.Property(b => b.Centroid)
                    .HasColumnType("vector(384)")
                    .HasConversion(
                        v => v == null ? null : new Vector(v),
                        v => v == null ? null : v.ToArray()
                    )
                    .Metadata.SetValueComparer(floatArrayComparer);

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

        modelBuilder.Ignore<Winnow.Integrations.Domain.IntegrationConfig>();

        // Organization -> Team -> Project hierarchy
        modelBuilder.ApplyConfiguration(new Configurations.OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TeamConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ReportConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ClusterConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AssetConfiguration());

        modelBuilder.ApplyConfiguration(new Configurations.OrganizationMemberConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TeamMemberConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProjectMemberConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrganizationInvitationConfiguration());

        // RBAC
        modelBuilder.ApplyConfiguration(new Configurations.RoleConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.RolePermissionConfiguration());

        var encryptedConverter = new EncryptedStringConverter(_encryptionKey);
        modelBuilder.ApplyConfiguration(new Configurations.IntegrationConfiguration(encryptedConverter));

        // MassTransit Outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Note: Global query filters for tenant isolation are applied at runtime
        // via the ITenantContext service, not at model creation time.
        // This avoids model drift issues when migrations are created with different
        // tenant context values than what's used at runtime.
        // The tenantContext.CurrentOrganizationId is used in the runtime context
        // to apply filters dynamically.
    }

    public DbSet<Domain.Organizations.Organization> Organizations { get; set; } = null!;
    public DbSet<Domain.Teams.Team> Teams { get; set; } = null!;
    public DbSet<Domain.Teams.TeamMember> TeamMembers { get; set; } = null!;
    public DbSet<Domain.Organizations.OrganizationMember> OrganizationMembers { get; set; } = null!;
    public DbSet<Domain.Reports.Report> Reports { get; set; } = null!;
    public DbSet<Domain.Clusters.Cluster> Clusters { get; set; } = null!;
    public DbSet<Domain.Assets.Asset> Assets { get; set; } = null!;
    public DbSet<Domain.Integrations.Integration> Integrations { get; set; } = null!;
    public DbSet<Domain.Projects.Project> Projects { get; set; } = null!;
    public DbSet<Domain.Projects.ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<Domain.Organizations.OrganizationInvitation> OrganizationInvitations { get; set; } = null!;

    // RBAC
    public new DbSet<Domain.Security.Role> Roles { get; set; } = null!;
    public DbSet<Domain.Security.Permission> Permissions { get; set; } = null!;
    public DbSet<Domain.Security.RolePermission> RolePermissions { get; set; } = null!;
}
