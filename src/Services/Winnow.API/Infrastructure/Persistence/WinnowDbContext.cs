using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Winnow.API.Infrastructure.Identity;
using Winnow.API.Infrastructure.MultiTenancy;
using Winnow.API.Infrastructure.Security;
using Winnow.API.Domain.Ai;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
            builder.HasPostgresExtension("vector");

            builder.Entity<Domain.Reports.Report>(entity =>
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

                // Global query filter to ensure un-sanitized reports are completely hidden from UI
                entity.HasQueryFilter(r => r.IsSanitized);
            });

            builder.Entity<Domain.Clusters.Cluster>(entity =>
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

        builder.Ignore<Winnow.Integrations.Domain.IntegrationConfig>();

        // Organization -> Team -> Project hierarchy
        builder.ApplyConfiguration(new Configurations.OrganizationConfiguration());
        builder.ApplyConfiguration(new Configurations.TeamConfiguration());
        builder.ApplyConfiguration(new Configurations.ProjectConfiguration());
        builder.ApplyConfiguration(new Configurations.ReportConfiguration());
        builder.ApplyConfiguration(new Configurations.ClusterConfiguration());
        builder.ApplyConfiguration(new Configurations.AssetConfiguration());

        builder.ApplyConfiguration(new Configurations.OrganizationMemberConfiguration());
        builder.ApplyConfiguration(new Configurations.TeamMemberConfiguration());
        builder.ApplyConfiguration(new Configurations.ProjectMemberConfiguration());
        builder.ApplyConfiguration(new Configurations.OrganizationInvitationConfiguration());
        builder.ApplyConfiguration(new Configurations.AiUsageConfiguration());

        // RBAC
        builder.ApplyConfiguration(new Configurations.RoleConfiguration());
        builder.ApplyConfiguration(new Configurations.PermissionConfiguration());
        builder.ApplyConfiguration(new Configurations.RolePermissionConfiguration());

        var encryptedConverter = new EncryptedStringConverter(_encryptionKey);
        builder.ApplyConfiguration(new Configurations.IntegrationConfiguration(encryptedConverter));


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
    public DbSet<Domain.Ai.AiUsage> AiUsages { get; set; } = null!;

    // RBAC
    public new DbSet<Domain.Security.Role> Roles { get; set; } = null!;
    public DbSet<Domain.Security.Permission> Permissions { get; set; } = null!;
    public DbSet<Domain.Security.RolePermission> RolePermissions { get; set; } = null!;
}
