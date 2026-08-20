using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequest>
{
    public void Configure(EntityTypeBuilder<VerificationRequest> builder)
    {
        builder.ToTable("VerificationRequests");

        builder.HasKey(vr => vr.Id);

        builder.Property(vr => vr.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(vr => vr.UserId)
            .IsRequired();

        builder.Property(vr => vr.BadgeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(vr => vr.RequestedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(vr => vr.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(vr => vr.SubmittedEvidence)
            .HasColumnType("nvarchar(max)");

        builder.Property(vr => vr.ReviewedBy);

        builder.Property(vr => vr.ReviewedAt);

        builder.Property(vr => vr.ReviewNotes)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(vr => vr.User)
            .WithMany(u => u.VerificationRequests)
            .HasForeignKey(vr => vr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vr => vr.ReviewerUser)
            .WithMany(u => u.ReviewedVerificationRequests)
            .HasForeignKey(vr => vr.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(vr => vr.UserId)
            .HasDatabaseName("IX_VerificationRequests_UserId");

        builder.HasIndex(vr => vr.BadgeType)
            .HasDatabaseName("IX_VerificationRequests_BadgeType");

        builder.HasIndex(vr => vr.Status)
            .HasDatabaseName("IX_VerificationRequests_Status");

        builder.HasIndex(vr => vr.RequestedAt)
            .HasDatabaseName("IX_VerificationRequests_RequestedAt");

        builder.HasIndex(vr => new { vr.UserId, vr.BadgeType, vr.Status })
            .HasDatabaseName("IX_VerificationRequests_UserId_BadgeType_Status");
    }
}