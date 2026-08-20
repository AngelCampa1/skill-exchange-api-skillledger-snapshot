using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectReviewConfiguration : IEntityTypeConfiguration<ProjectReview>
{
    public void Configure(EntityTypeBuilder<ProjectReview> builder)
    {
        builder.ToTable("ProjectReviews");

        // Primary Key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(r => r.ProjectId)
            .IsRequired();

        builder.Property(r => r.ReviewerId)
            .IsRequired();

        builder.Property(r => r.RevieweeId)
            .IsRequired();

        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.OverallRating)
            .IsRequired();

        builder.Property(r => r.QualityRating);

        builder.Property(r => r.CommunicationRating);

        builder.Property(r => r.TimelinessRating);

        builder.Property(r => r.ProfessionalismRating);

        builder.Property(r => r.ReviewText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.ResponseText)
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ProjectReviewStatus.Pending);

        builder.Property(r => r.ModerationStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ModerationStatus.Pending);

        builder.Property(r => r.ModerationNotes)
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.SubmittedAt);

        builder.Property(r => r.PublishedAt);

        builder.Property(r => r.SubmittedFromIP)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(r => r.HasPhotoAttachments)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.PhotoAttachmentCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Computed Properties (ignored in database)
        builder.Ignore(r => r.CalculatedAverageRating);
        builder.Ignore(r => r.IsSelfReview);
        builder.Ignore(r => r.IsEditable);
        builder.Ignore(r => r.CanBeRetracted);
        builder.Ignore(r => r.IsVisible);
        builder.Ignore(r => r.IsUnderModeration);

        // Indexes for performance
        builder.HasIndex(r => r.ProjectId)
            .HasDatabaseName("IX_ProjectReviews_ProjectId");

        builder.HasIndex(r => r.ReviewerId)
            .HasDatabaseName("IX_ProjectReviews_ReviewerId");

        builder.HasIndex(r => r.RevieweeId)
            .HasDatabaseName("IX_ProjectReviews_RevieweeId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_ProjectReviews_Status");

        builder.HasIndex(r => r.ModerationStatus)
            .HasDatabaseName("IX_ProjectReviews_ModerationStatus");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_ProjectReviews_CreatedAt");

        builder.HasIndex(r => r.PublishedAt)
            .HasDatabaseName("IX_ProjectReviews_PublishedAt");

        // Compound indexes for common queries
        builder.HasIndex(r => new { r.ProjectId, r.Type })
            .HasDatabaseName("IX_ProjectReviews_ProjectId_Type");

        builder.HasIndex(r => new { r.RevieweeId, r.Status })
            .HasDatabaseName("IX_ProjectReviews_RevieweeId_Status");

        builder.HasIndex(r => new { r.Status, r.ModerationStatus })
            .HasDatabaseName("IX_ProjectReviews_Status_ModerationStatus");

        // Unique constraint to prevent duplicate reviews (one review per reviewer per project per type)
        builder.HasIndex(r => new { r.ProjectId, r.ReviewerId, r.Type })
            .IsUnique()
            .HasDatabaseName("UX_ProjectReviews_ProjectId_ReviewerId_Type");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_OverallRating",
            "[OverallRating] >= 1 AND [OverallRating] <= 10"));

        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_QualityRating",
            "[QualityRating] IS NULL OR ([QualityRating] >= 1 AND [QualityRating] <= 10)"));

        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_CommunicationRating",
            "[CommunicationRating] IS NULL OR ([CommunicationRating] >= 1 AND [CommunicationRating] <= 10)"));

        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_TimelinessRating",
            "[TimelinessRating] IS NULL OR ([TimelinessRating] >= 1 AND [TimelinessRating] <= 10)"));

        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_ProfessionalismRating",
            "[ProfessionalismRating] IS NULL OR ([ProfessionalismRating] >= 1 AND [ProfessionalismRating] <= 10)"));

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_NoSelfReview",
            "[ReviewerId] != [RevieweeId]"));

        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_PhotoAttachmentCount",
            "[PhotoAttachmentCount] >= 0 AND ([HasPhotoAttachments] = 0 OR [PhotoAttachmentCount] > 0)"));

        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectReviews_ReviewTextLength",
            "LEN(LTRIM(RTRIM([ReviewText]))) >= 25"));

        // Relationships
        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete when user is deleted

        builder.HasOne(r => r.Reviewee)
            .WithMany()
            .HasForeignKey(r => r.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete when user is deleted

        builder.HasMany(r => r.PhotoAttachments)
            .WithOne()
            .HasForeignKey("ProjectReviewId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.AuditLogs)
            .WithOne()
            .HasForeignKey("ProjectReviewId")
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete audit logs
    }
}