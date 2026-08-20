using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class CategoryReputationScoresConfiguration : IEntityTypeConfiguration<CategoryReputationScore>
{
    public void Configure(EntityTypeBuilder<CategoryReputationScore> builder)
    {
        builder.ToTable("CategoryReputationScores");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.SkillId)
            .IsRequired();

        builder.Property(c => c.Score)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(c => c.ProjectCount)
            .IsRequired();

        builder.Property(c => c.LastProjectAt)
            .HasColumnType("datetime2");

        // Relationships
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Skill)
            .WithMany()
            .HasForeignKey(c => c.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_CategoryReputationScores_UserId");

        builder.HasIndex(c => c.SkillId)
            .HasDatabaseName("IX_CategoryReputationScores_SkillId");

        builder.HasIndex(c => new { c.UserId, c.SkillId })
            .IsUnique()
            .HasDatabaseName("IX_CategoryReputationScores_UserSkill");

        builder.HasIndex(c => c.Score)
            .HasDatabaseName("IX_CategoryReputationScores_Score");
    }
}