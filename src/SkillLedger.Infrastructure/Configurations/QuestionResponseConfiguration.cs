using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class QuestionResponseConfiguration : IEntityTypeConfiguration<QuestionResponse>
{
    public void Configure(EntityTypeBuilder<QuestionResponse> builder)
    {
        // Primary key
        builder.HasKey(qr => qr.Id);

        // Properties
        builder.Property(qr => qr.ResponseValue)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(qr => qr.SelectedOptionIds)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(qr => qr.FileAttachments)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(qr => qr.Metadata)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(qr => qr.IsValid)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(qr => qr.ValidationError)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(qr => qr.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(qr => qr.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(qr => qr.QuestionnaireResponseId)
            .HasDatabaseName("IX_QuestionResponses_QuestionnaireResponseId");

        builder.HasIndex(qr => qr.QuestionId)
            .HasDatabaseName("IX_QuestionResponses_QuestionId");

        builder.HasIndex(qr => qr.IsValid)
            .HasDatabaseName("IX_QuestionResponses_IsValid");

        builder.HasIndex(qr => qr.CreatedAt)
            .HasDatabaseName("IX_QuestionResponses_CreatedAt");

        builder.HasIndex(qr => qr.UpdatedAt)
            .HasDatabaseName("IX_QuestionResponses_UpdatedAt");

        // Composite indexes for common queries
        builder.HasIndex(qr => new { qr.QuestionnaireResponseId, qr.QuestionId })
            .HasDatabaseName("IX_QuestionResponses_Response_Question")
            .IsUnique(); // One response per question per questionnaire response

        builder.HasIndex(qr => new { qr.QuestionId, qr.IsValid, qr.UpdatedAt })
            .HasDatabaseName("IX_QuestionResponses_Question_Valid_Updated");

        // Relationships
        builder.HasOne(qr => qr.QuestionnaireResponse)
            .WithMany(r => r.QuestionResponses)
            .HasForeignKey(qr => qr.QuestionnaireResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qr => qr.Question)
            .WithMany(q => q.Responses)
            .HasForeignKey(qr => qr.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("QuestionResponses");
    }
}