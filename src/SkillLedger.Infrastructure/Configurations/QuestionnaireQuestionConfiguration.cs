using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class QuestionnaireQuestionConfiguration : IEntityTypeConfiguration<QuestionnaireQuestion>
{
    public void Configure(EntityTypeBuilder<QuestionnaireQuestion> builder)
    {
        // Primary key
        builder.HasKey(q => q.Id);

        // Properties
        builder.Property(q => q.QuestionText)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(q => q.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(q => q.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(q => q.IsRequired)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(q => q.DisplayOrder)
            .IsRequired();

        builder.Property(q => q.Configuration)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(q => q.DefaultValue)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(q => q.PlaceholderText)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(q => q.ValidationRegex)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(q => q.ValidationMessage)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(q => q.MinValue)
            .IsRequired(false);

        builder.Property(q => q.MaxValue)
            .IsRequired(false);

        builder.Property(q => q.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(q => q.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(q => q.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(q => q.QuestionnaireId)
            .HasDatabaseName("IX_QuestionnaireQuestions_QuestionnaireId");

        builder.HasIndex(q => q.Type)
            .HasDatabaseName("IX_QuestionnaireQuestions_Type");

        builder.HasIndex(q => q.IsActive)
            .HasDatabaseName("IX_QuestionnaireQuestions_IsActive");

        builder.HasIndex(q => q.IsRequired)
            .HasDatabaseName("IX_QuestionnaireQuestions_IsRequired");

        builder.HasIndex(q => q.DisplayOrder)
            .HasDatabaseName("IX_QuestionnaireQuestions_DisplayOrder");

        // Composite indexes for common queries
        builder.HasIndex(q => new { q.QuestionnaireId, q.IsActive, q.DisplayOrder })
            .HasDatabaseName("IX_QuestionnaireQuestions_Questionnaire_Active_Order");

        builder.HasIndex(q => new { q.QuestionnaireId, q.IsRequired, q.IsActive })
            .HasDatabaseName("IX_QuestionnaireQuestions_Questionnaire_Required_Active");

        // Relationships
        builder.HasOne(q => q.Questionnaire)
            .WithMany(qu => qu.Questions)
            .HasForeignKey(q => q.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Responses)
            .WithOne(r => r.Question)
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("QuestionnaireQuestions");
    }
}