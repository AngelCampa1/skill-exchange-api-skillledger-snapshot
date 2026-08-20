using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class QuestionnaireResponseConfiguration : IEntityTypeConfiguration<QuestionnaireResponse>
{
    public void Configure(EntityTypeBuilder<QuestionnaireResponse> builder)
    {
        // Primary key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.IsSubmitted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.IsComplete)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.StartedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(r => r.SubmittedAt)
            .IsRequired(false);

        builder.Property(r => r.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(r => r.SubmittedFromIP)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired(false);

        builder.Property(r => r.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.Metadata)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(r => r.ReviewNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(r => r.ReviewedAt)
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(r => r.QuestionnaireId)
            .HasDatabaseName("IX_QuestionnaireResponses_QuestionnaireId");

        builder.HasIndex(r => r.RespondentUserId)
            .HasDatabaseName("IX_QuestionnaireResponses_RespondentUserId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_QuestionnaireResponses_Status");

        builder.HasIndex(r => r.IsSubmitted)
            .HasDatabaseName("IX_QuestionnaireResponses_IsSubmitted");

        builder.HasIndex(r => r.IsComplete)
            .HasDatabaseName("IX_QuestionnaireResponses_IsComplete");

        builder.HasIndex(r => r.StartedAt)
            .HasDatabaseName("IX_QuestionnaireResponses_StartedAt");

        builder.HasIndex(r => r.SubmittedAt)
            .HasDatabaseName("IX_QuestionnaireResponses_SubmittedAt");

        builder.HasIndex(r => r.UpdatedAt)
            .HasDatabaseName("IX_QuestionnaireResponses_UpdatedAt");

        builder.HasIndex(r => r.ReviewedByUserId)
            .HasDatabaseName("IX_QuestionnaireResponses_ReviewedByUserId");

        builder.HasIndex(r => r.ReviewedAt)
            .HasDatabaseName("IX_QuestionnaireResponses_ReviewedAt");

        // Composite indexes for common queries
        builder.HasIndex(r => new { r.QuestionnaireId, r.Status, r.SubmittedAt })
            .HasDatabaseName("IX_QuestionnaireResponses_Questionnaire_Status_Submitted");

        builder.HasIndex(r => new { r.RespondentUserId, r.IsSubmitted, r.UpdatedAt })
            .HasDatabaseName("IX_QuestionnaireResponses_Respondent_Submitted_Updated");

        builder.HasIndex(r => new { r.QuestionnaireId, r.RespondentUserId, r.Status })
            .HasDatabaseName("IX_QuestionnaireResponses_Questionnaire_Respondent_Status");

        builder.HasIndex(r => new { r.Status, r.ReviewedByUserId, r.ReviewedAt })
            .HasDatabaseName("IX_QuestionnaireResponses_Status_Reviewer_Reviewed");

        // Unique constraint to prevent duplicate submissions (one response per user per questionnaire when submitted)
        builder.HasIndex(r => new { r.QuestionnaireId, r.RespondentUserId })
            .HasDatabaseName("IX_QuestionnaireResponses_Unique_Submission")
            .HasFilter("[IsSubmitted] = 1")
            .IsUnique();

        // Relationships
        builder.HasOne(r => r.Questionnaire)
            .WithMany(q => q.Responses)
            .HasForeignKey(r => r.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RespondentUser)
            .WithMany()
            .HasForeignKey(r => r.RespondentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.QuestionResponses)
            .WithOne(qr => qr.QuestionnaireResponse)
            .HasForeignKey(qr => qr.QuestionnaireResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("QuestionnaireResponses");
    }
}