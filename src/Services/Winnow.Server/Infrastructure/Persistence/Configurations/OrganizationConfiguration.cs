using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Winnow.Server.Domain.Common;
using Winnow.Server.Domain.Organizations;
using Winnow.Server.Domain.Organizations.ValueObjects;

namespace Winnow.Server.Infrastructure.Persistence.Configurations;

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

        // Ignore DomainEvents since they aren't mapped to the database
        builder.Ignore(o => o.DomainEvents);
    }
}
