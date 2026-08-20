using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class QuestionnaireConfiguration : IEntityTypeConfiguration<Questionnaire>
{
    public void Configure(EntityTypeBuilder<Questionnaire> builder)
    {
        // Primary key
        builder.HasKey(q => q.Id);

        // Properties
        builder.Property(q => q.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(q => q.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(q => q.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(q => q.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(q => q.IsTemplate)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(q => q.RequiresReview)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(q => q.MaxResponses)
            .IsRequired(false);

        builder.Property(q => q.StartDate)
            .IsRequired(false);

        builder.Property(q => q.EndDate)
            .IsRequired(false);

        builder.Property(q => q.Version)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(q => q.Metadata)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(q => q.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(q => q.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(q => q.CreatedByUserId)
            .HasDatabaseName("IX_Questionnaires_CreatedByUserId");

        builder.HasIndex(q => q.Type)
            .HasDatabaseName("IX_Questionnaires_Type");

        builder.HasIndex(q => q.IsActive)
            .HasDatabaseName("IX_Questionnaires_IsActive");

        builder.HasIndex(q => q.IsTemplate)
            .HasDatabaseName("IX_Questionnaires_IsTemplate");

        builder.HasIndex(q => q.CreatedAt)
            .HasDatabaseName("IX_Questionnaires_CreatedAt");

        builder.HasIndex(q => q.UpdatedAt)
            .HasDatabaseName("IX_Questionnaires_UpdatedAt");

        builder.HasIndex(q => q.StartDate)
            .HasDatabaseName("IX_Questionnaires_StartDate");

        builder.HasIndex(q => q.EndDate)
            .HasDatabaseName("IX_Questionnaires_EndDate");

        // Composite indexes for common queries
        builder.HasIndex(q => new { q.IsActive, q.Type, q.CreatedAt })
            .HasDatabaseName("IX_Questionnaires_Active_Type_Created");

        builder.HasIndex(q => new { q.IsTemplate, q.IsActive, q.UpdatedAt })
            .HasDatabaseName("IX_Questionnaires_Template_Active_Updated");

        builder.HasIndex(q => new { q.CreatedByUserId, q.IsActive, q.UpdatedAt })
            .HasDatabaseName("IX_Questionnaires_CreatedBy_Active_Updated");

        // Relationships
        builder.HasOne(q => q.CreatedByUser)
            .WithMany()
            .HasForeignKey(q => q.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Questions)
            .WithOne(qu => qu.Questionnaire)
            .HasForeignKey(qu => qu.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Responses)
            .WithOne(r => r.Questionnaire)
            .HasForeignKey(r => r.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("Questionnaires");
    }
}