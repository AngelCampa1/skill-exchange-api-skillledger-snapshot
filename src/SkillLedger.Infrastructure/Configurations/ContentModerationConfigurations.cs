using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for ContentModerationLog
/// </summary>
public class ContentModerationLogConfiguration : IEntityTypeConfiguration<ContentModerationLog>
{
    public void Configure(EntityTypeBuilder<ContentModerationLog> builder)
    {
        builder.ToTable("ContentModerationLogs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.ContentType)
            .IsRequired();

        builder.Property(c => c.WasApproved)
            .IsRequired();

        builder.Property(c => c.RiskLevel)
            .IsRequired();

        builder.Property(c => c.RequiredHumanReview)
            .IsRequired();

        builder.Property(c => c.ReasonForRejection)
            .HasMaxLength(500);

        builder.Property(c => c.AnalysisId)
            .HasMaxLength(100);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.WasApproved);
    }
}

/// <summary>
/// Entity configuration for CustomBlocklistTerm
/// </summary>
public class CustomBlocklistTermConfiguration : IEntityTypeConfiguration<CustomBlocklistTerm>
{
    public void Configure(EntityTypeBuilder<CustomBlocklistTerm> builder)
    {
        builder.ToTable("CustomBlocklistTerms");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Term)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.AddedByUserId)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasOne(c => c.AddedByUser)
            .WithMany()
            .HasForeignKey(c => c.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Term);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.ExpiresAt);
    }
}

/// <summary>
/// Entity configuration for ContentReviewQueue
/// </summary>
public class ContentReviewQueueConfiguration : IEntityTypeConfiguration<ContentReviewQueue>
{
    public void Configure(EntityTypeBuilder<ContentReviewQueue> builder)
    {
        builder.ToTable("ContentReviewQueues");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.ContentType)
            .IsRequired();

        builder.Property(c => c.ContentUrl)
            .HasMaxLength(500);

        builder.Property(c => c.ReviewPriority)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.AssignedReviewer)
            .WithMany()
            .HasForeignKey(c => c.AssignedReviewerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.ReviewPriority);
        builder.HasIndex(c => c.CreatedAt);
    }
}