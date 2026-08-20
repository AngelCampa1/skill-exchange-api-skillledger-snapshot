using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("UserBadges");

        builder.HasKey(ub => ub.Id);

        builder.Property(ub => ub.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(ub => ub.UserId)
            .IsRequired();

        builder.Property(ub => ub.BadgeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ub => ub.BadgeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ub => ub.BadgeDescription)
            .HasMaxLength(500);

        builder.Property(ub => ub.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(ub => ub.IconUrl)
            .HasMaxLength(500);

        builder.Property(ub => ub.EarnedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ub => ub.ExpiresAt);

        builder.Property(ub => ub.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ub => ub.VerificationLevel)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(ub => ub.VerificationEvidence)
            .HasColumnType("nvarchar(max)");

        builder.Property(ub => ub.VerifiedBy);

        builder.Property(ub => ub.VerifiedAt);

        builder.Property(ub => ub.IntegrityHash)
            .HasMaxLength(256);

        // Relationships
        builder.HasOne(ub => ub.User)
            .WithMany(u => u.Badges)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.VerifierUser)
            .WithMany(u => u.VerifiedBadges)
            .HasForeignKey(ub => ub.VerifiedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ub => ub.UserId)
            .HasDatabaseName("IX_UserBadges_UserId");

        builder.HasIndex(ub => ub.BadgeType)
            .HasDatabaseName("IX_UserBadges_BadgeType");

        // VULN-032 FIX: Add unique constraint to prevent duplicate badge awards
        builder.HasIndex(ub => new { ub.UserId, ub.BadgeType, ub.IsActive })
            .HasDatabaseName("IX_UserBadges_UserId_BadgeType_IsActive")
            .IsUnique()
            .HasFilter("[IsActive] = 1"); // Only enforce uniqueness for active badges

        builder.HasIndex(ub => ub.EarnedAt)
            .HasDatabaseName("IX_UserBadges_EarnedAt");

        builder.HasIndex(ub => ub.ExpiresAt)
            .HasDatabaseName("IX_UserBadges_ExpiresAt");

        builder.HasIndex(ub => new { ub.IsActive, ub.ExpiresAt })
            .HasDatabaseName("IX_UserBadges_IsActive_ExpiresAt");
    }
}