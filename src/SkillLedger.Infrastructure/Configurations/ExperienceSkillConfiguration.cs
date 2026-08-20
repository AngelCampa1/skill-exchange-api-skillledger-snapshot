using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ExperienceSkillConfiguration : IEntityTypeConfiguration<ExperienceSkill>
{
    public void Configure(EntityTypeBuilder<ExperienceSkill> builder)
    {
        builder.ToTable("ExperienceSkills");

        // Primary Key
        builder.HasKey(es => es.Id);

        // Properties
        builder.Property(es => es.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(es => es.ExperienceId)
            .IsRequired();

        builder.Property(es => es.SkillId)
            .IsRequired();

        builder.Property(es => es.Notes)
            .HasMaxLength(500);

        builder.Property(es => es.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(es => es.ExperienceId)
            .HasDatabaseName("IX_ExperienceSkills_ExperienceId");

        builder.HasIndex(es => es.SkillId)
            .HasDatabaseName("IX_ExperienceSkills_SkillId");

        builder.HasIndex(es => new { es.ExperienceId, es.SkillId })
            .IsUnique()
            .HasDatabaseName("IX_ExperienceSkills_ExperienceId_SkillId");

        // Relationships
        builder.HasOne(es => es.Experience)
            .WithMany(e => e.ExperienceSkills)
            .HasForeignKey(es => es.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(es => es.Skill)
            .WithMany()
            .HasForeignKey(es => es.SkillId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete skills when experiences are deleted
    }
}