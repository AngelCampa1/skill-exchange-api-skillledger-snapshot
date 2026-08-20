using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        // Primary key
        builder.HasKey(us => us.Id);

        // Properties
        builder.Property(us => us.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(us => us.StartDate)
            .IsRequired();

        builder.Property(us => us.EndDate)
            .IsRequired(false);

        builder.Property(us => us.NextBillingDate)
            .IsRequired(false);

        builder.Property(us => us.TrialEndDate)
            .IsRequired(false);

        builder.Property(us => us.AutoRenew)
            .IsRequired();

        builder.Property(us => us.ExternalSubscriptionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(us => us.ExternalCustomerId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(us => us.BillingCycleCount)
            .IsRequired();

        builder.Property(us => us.IsAnnual)
            .IsRequired();

        builder.Property(us => us.RetryCount)
            .IsRequired();

        builder.Property(us => us.NextRetryAt)
            .IsRequired(false);

        builder.Property(us => us.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(us => us.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(us => us.CancelledAt)
            .IsRequired(false);

        builder.Property(us => us.CancellationReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(us => us.LastPaymentDate)
            .IsRequired(false);

        // Promotion Tracking Fields
        builder.Property(us => us.AppliedCouponId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(us => us.AppliedPromoCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(us => us.DiscountEndsAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(us => us.UserId)
            .HasDatabaseName("IX_UserSubscriptions_UserId");

        builder.HasIndex(us => us.SubscriptionTierId)
            .HasDatabaseName("IX_UserSubscriptions_SubscriptionTierId");

        builder.HasIndex(us => us.PaymentMethodId)
            .HasDatabaseName("IX_UserSubscriptions_PaymentMethodId");

        builder.HasIndex(us => new { us.UserId, us.Status })
            .HasDatabaseName("IX_UserSubscriptions_UserId_Status");

        builder.HasIndex(us => us.NextBillingDate)
            .HasDatabaseName("IX_UserSubscriptions_NextBillingDate")
            .HasFilter("[NextBillingDate] IS NOT NULL");

        builder.HasIndex(us => us.ExternalSubscriptionId)
            .HasDatabaseName("IX_UserSubscriptions_ExternalSubscriptionId")
            .HasFilter("[ExternalSubscriptionId] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(us => us.ExternalCustomerId)
            .HasDatabaseName("IX_UserSubscriptions_ExternalCustomerId");

        builder.HasIndex(us => us.AppliedCouponId)
            .HasDatabaseName("IX_UserSubscriptions_AppliedCouponId")
            .HasFilter("[AppliedCouponId] IS NOT NULL");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserSubscriptions_BillingCycleCount_NonNegative",
            "[BillingCycleCount] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserSubscriptions_StartDate_Before_EndDate",
            "[EndDate] IS NULL OR [StartDate] <= [EndDate]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserSubscriptions_TrialEndDate_After_StartDate",
            "[TrialEndDate] IS NULL OR [StartDate] <= [TrialEndDate]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserSubscriptions_NextBillingDate_After_StartDate",
            "[NextBillingDate] IS NULL OR [StartDate] <= [NextBillingDate]"));

        // Unique constraint: One active subscription per user
        builder.HasIndex(us => new { us.UserId, us.Status })
            .HasDatabaseName("IX_UserSubscriptions_UserId_ActiveStatus")
            .HasFilter("[Status] IN (1, 2)")
            .IsUnique();

        // Relationships
        // Note: User relationship removed to avoid cascade path conflicts
        // Will be managed at application level

        builder.HasOne(us => us.SubscriptionTier)
            .WithMany(st => st.UserSubscriptions)
            .HasForeignKey(us => us.SubscriptionTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(us => us.PaymentMethod)
            .WithMany(pm => pm.Subscriptions)
            .HasForeignKey(us => us.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}