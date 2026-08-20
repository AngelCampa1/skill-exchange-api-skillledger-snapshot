using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class SubscriptionTierConfiguration : IEntityTypeConfiguration<SubscriptionTier>
{
    public void Configure(EntityTypeBuilder<SubscriptionTier> builder)
    {
        // Primary key
        builder.HasKey(st => st.Id);

        // Properties
        builder.Property(st => st.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(st => st.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(st => st.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(st => st.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(st => st.AnnualPrice)
            .HasPrecision(10, 2)
            .IsRequired(false);

        builder.Property(st => st.CreditBonus)
            .IsRequired();

        builder.Property(st => st.MaxActiveProjects)
            .IsRequired();

        builder.Property(st => st.MaxTeamMembers)
            .IsRequired();

        builder.Property(st => st.PrioritySupport)
            .IsRequired();

        builder.Property(st => st.ApiAccess)
            .IsRequired();

        builder.Property(st => st.AdvancedAnalytics)
            .IsRequired();

        builder.Property(st => st.AdvancedFraudDetection)
            .IsRequired();

        builder.Property(st => st.MultiSignature)
            .IsRequired();

        builder.Property(st => st.CustomIntegrations)
            .IsRequired();

        builder.Property(st => st.MaxMonthlyEarnings)
            .IsRequired();

        builder.Property(st => st.Features)
            .IsRequired(false);

        builder.Property(st => st.IsActive)
            .IsRequired();

        builder.Property(st => st.SortOrder)
            .IsRequired();

        builder.Property(st => st.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(st => st.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(st => st.Type)
            .HasDatabaseName("IX_SubscriptionTiers_Type")
            .IsUnique();

        builder.HasIndex(st => new { st.IsActive, st.SortOrder })
            .HasDatabaseName("IX_SubscriptionTiers_IsActive_SortOrder");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_Price_Positive",
            "[Price] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_AnnualPrice_Positive",
            "[AnnualPrice] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_CreditBonus_NonNegative",
            "[CreditBonus] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_MaxActiveProjects_Positive",
            "[MaxActiveProjects] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_MaxTeamMembers_NonNegative",
            "[MaxTeamMembers] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_MaxMonthlyEarnings_NonNegative",
            "[MaxMonthlyEarnings] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTiers_SortOrder_NonNegative",
            "[SortOrder] >= 0"));

        // Relationships
        builder.HasMany(st => st.UserSubscriptions)
            .WithOne(us => us.SubscriptionTier)
            .HasForeignKey(us => us.SubscriptionTierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}