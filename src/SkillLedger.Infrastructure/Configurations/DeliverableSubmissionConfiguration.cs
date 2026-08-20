using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for DeliverableSubmission entity
/// </summary>
public class DeliverableSubmissionConfiguration : IEntityTypeConfiguration<DeliverableSubmission>
{
    public void Configure(EntityTypeBuilder<DeliverableSubmission> builder)
    {
        // Table configuration
        builder.ToTable("DeliverableSubmissions");

        // Primary key
        builder.HasKey(ds => ds.Id);

        // Properties
        builder.Property(ds => ds.Id)
            .IsRequired()
            .HasComment("Unique identifier for the deliverable submission");

        builder.Property(ds => ds.MilestoneId)
            .IsRequired()
            .HasComment("Foreign key to the associated milestone");

        builder.Property(ds => ds.SubmittedByUserId)
            .IsRequired()
            .HasComment("User who submitted this deliverable");

        builder.Property(ds => ds.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasComment("Type of deliverable submission");

        builder.Property(ds => ds.Title)
            .IsRequired()
            .HasMaxLength(300)
            .HasComment("Title or summary of the submission");

        builder.Property(ds => ds.Description)
            .HasMaxLength(5000)
            .HasComment("Detailed description of submitted work");

        builder.Property(ds => ds.SubmissionUrl)
            .HasMaxLength(2000)
            .HasComment("URL for link or repository submissions");

        builder.Property(ds => ds.TextContent)
            .HasComment("Text content for text-type submissions");

        builder.Property(ds => ds.SubmittedAt)
            .IsRequired()
            .HasComment("When the submission was created");

        builder.Property(ds => ds.SubmittedFromIP)
            .HasMaxLength(45)
            .HasComment("IP address from which submission was made");

        builder.Property(ds => ds.SubmissionNotes)
            .HasMaxLength(2000)
            .HasComment("Optional notes from the submitter");

        builder.Property(ds => ds.IsReviewed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this submission has been reviewed");

        builder.Property(ds => ds.IsApproved)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this submission was approved");

        builder.Property(ds => ds.ReviewedAt)
            .HasComment("When this submission was reviewed");

        builder.Property(ds => ds.ReviewedByUserId)
            .HasComment("User who reviewed this submission");

        builder.Property(ds => ds.ReviewFeedback)
            .HasMaxLength(3000)
            .HasComment("Feedback from the reviewer");

        // Relationships
        builder.HasOne(ds => ds.Milestone)
            .WithMany(m => m.Submissions)
            .HasForeignKey(ds => ds.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_DeliverableSubmissions_ProjectMilestones");

        builder.HasOne(ds => ds.SubmittedByUser)
            .WithMany()
            .HasForeignKey(ds => ds.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DeliverableSubmissions_Users_SubmittedBy");

        builder.HasOne(ds => ds.ReviewedByUser)
            .WithMany()
            .HasForeignKey(ds => ds.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DeliverableSubmissions_Users_ReviewedBy");

        // Many-to-many relationship with UploadedFiles
        builder.HasMany(ds => ds.AttachedFiles)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "DeliverableSubmissionFiles",
                j => j.HasOne<UploadedFile>().WithMany().HasForeignKey("FileId"),
                j => j.HasOne<DeliverableSubmission>().WithMany().HasForeignKey("SubmissionId"),
                j =>
                {
                    j.HasKey("SubmissionId", "FileId");
                    j.ToTable("DeliverableSubmissionFiles");
                    j.HasIndex("SubmissionId");
                    j.HasIndex("FileId");
                });

        // Indexes for performance
        builder.HasIndex(ds => ds.MilestoneId)
            .HasDatabaseName("IX_DeliverableSubmissions_MilestoneId");

        builder.HasIndex(ds => ds.SubmittedByUserId)
            .HasDatabaseName("IX_DeliverableSubmissions_SubmittedBy");

        builder.HasIndex(ds => ds.SubmittedAt)
            .HasDatabaseName("IX_DeliverableSubmissions_SubmittedAt");

        builder.HasIndex(ds => ds.IsReviewed)
            .HasDatabaseName("IX_DeliverableSubmissions_IsReviewed");

        builder.HasIndex(ds => new { ds.MilestoneId, ds.SubmittedAt })
            .HasDatabaseName("IX_DeliverableSubmissions_Milestone_SubmittedAt");

        builder.HasIndex(ds => new { ds.SubmittedByUserId, ds.SubmittedAt })
            .HasDatabaseName("IX_DeliverableSubmissions_User_SubmittedAt");

        // Composite index for filtering by review status
        builder.HasIndex(ds => new { ds.IsReviewed, ds.IsApproved, ds.SubmittedAt })
            .HasDatabaseName("IX_DeliverableSubmissions_Review_Status");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DeliverableSubmissions_Title_NotEmpty",
            "LEN(TRIM([Title])) > 0"));

        // Review logic constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DeliverableSubmissions_Review_Logic",
            "([IsReviewed] = 0) OR ([IsReviewed] = 1 AND [ReviewedAt] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DeliverableSubmissions_Approval_Logic",
            "([IsApproved] = 0) OR ([IsApproved] = 1 AND [IsReviewed] = 1)"));

        // Date constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DeliverableSubmissions_ReviewedAt_After_SubmittedAt",
            "[ReviewedAt] IS NULL OR [ReviewedAt] >= [SubmittedAt]"));
    }
}
