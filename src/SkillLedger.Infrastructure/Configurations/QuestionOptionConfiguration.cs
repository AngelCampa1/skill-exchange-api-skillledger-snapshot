using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        // Primary key
        builder.HasKey(o => o.Id);

        // Properties
        builder.Property(o => o.OptionText)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.OptionValue)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(o => o.DisplayOrder)
            .IsRequired();

        builder.Property(o => o.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(o => o.IsDefault)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(o => o.Metadata)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(o => o.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(o => o.QuestionId)
            .HasDatabaseName("IX_QuestionOptions_QuestionId");

        builder.HasIndex(o => o.IsActive)
            .HasDatabaseName("IX_QuestionOptions_IsActive");

        builder.HasIndex(o => o.IsDefault)
            .HasDatabaseName("IX_QuestionOptions_IsDefault");

        builder.HasIndex(o => o.DisplayOrder)
            .HasDatabaseName("IX_QuestionOptions_DisplayOrder");

        // Composite indexes for common queries
        builder.HasIndex(o => new { o.QuestionId, o.IsActive, o.DisplayOrder })
            .HasDatabaseName("IX_QuestionOptions_Question_Active_Order");

        builder.HasIndex(o => new { o.QuestionId, o.IsDefault, o.IsActive })
            .HasDatabaseName("IX_QuestionOptions_Question_Default_Active");

        // Relationships
        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("QuestionOptions");
    }
}