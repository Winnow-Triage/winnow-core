using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.API.Domain.Common;
using Winnow.API.Domain.Organizations;
using Winnow.API.Domain.Organizations.ValueObjects;

namespace Winnow.API.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the entity type for <see cref="Organization"/>.
/// </summary>
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    /// <summary>
    /// Configures the entity type for <see cref="Organization"/>.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        // Name is required and limited in length
        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.ContactEmail)
            .HasConversion(
                email => email.Value,
                value => new Email(value)
            )
            .HasColumnName("ContactEmail")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(o => o.Plan)
            .HasConversion(
                plan => plan.Name,
                dbString => SubscriptionPlan.FromName(dbString)
            )
            .HasColumnName("SubscriptionTier") // Keeps your database schema perfectly intact
            .IsRequired();

        // 2. Flatten the Report Quota into the Organizations table
        builder.ComplexProperty(o => o.ReportQuota, quota =>
        {
            // Map these to whatever your actual database columns are currently named
            quota.Property(q => q.Limit).HasColumnName("MonthlyReportLimit");
            quota.Property(q => q.Consumed).HasColumnName("CurrentMonthReports");
        });

        // 3. Flatten the Summary Quota into the Organizations table
        builder.ComplexProperty(o => o.SummaryQuota, quota =>
        {
            quota.Property(q => q.Limit).HasColumnName("MonthlySummaryLimit");
            quota.Property(q => q.Consumed).HasColumnName("CurrentMonthSummaries");
        });

        builder.ComplexProperty(o => o.BillingIdentity, billing =>
        {
            billing.Property(b => b.Provider).HasColumnName("BillingProvider");
            billing.Property(b => b.CustomerId).HasColumnName("StripeCustomerId");
            billing.Property(b => b.SubscriptionId).HasColumnName("StripeSubscriptionId");
        });

        // Map Settings as a JSON column for flexibility
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var settingsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<OrganizationSettings>(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) == JsonSerializer.Serialize(c2, jsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => JsonSerializer.Deserialize<OrganizationSettings>(JsonSerializer.Serialize(c, jsonOptions), jsonOptions)!
        );

        builder.Property(o => o.Settings)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<OrganizationSettings>(v, jsonOptions)!,
                settingsComparer
            );

        // EF Core Navigation Properties - Mapped to internal collections
        builder.HasMany(o => o.OrganizationTeams)
            .WithOne()
            .HasForeignKey(t => t.OrganizationId);
        builder.Navigation(o => o.OrganizationTeams).HasField("_teams");

        builder.HasMany(o => o.OrganizationProjects)
            .WithOne()
            .HasForeignKey(p => p.OrganizationId);
        builder.Navigation(o => o.OrganizationProjects).HasField("_projects");

        builder.HasMany(o => o.OrganizationMemberships)
            .WithOne()
            .HasForeignKey(m => m.OrganizationId);
        builder.Navigation(o => o.OrganizationMemberships).HasField("_memberships");

        // Ignore the DDD Guid lists for persistence since we use navigations instead
        builder.Ignore(o => o.Teams);
        builder.Ignore(o => o.Projects);
        builder.Ignore(o => o.Members);

        // Ignore DomainEvents since they aren't mapped to the database
        builder.Ignore(o => o.DomainEvents);
    }
}
