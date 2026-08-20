using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectSkillConfiguration : IEntityTypeConfiguration<ProjectSkill>
{
    public void Configure(EntityTypeBuilder<ProjectSkill> builder)
    {
        builder.ToTable("ProjectSkills");

        // Composite Primary Key
        builder.HasKey(ps => new { ps.ProjectId, ps.SkillId });

        // Properties
        builder.Property(ps => ps.ProjectId)
            .IsRequired();

        builder.Property(ps => ps.SkillId)
            .IsRequired();

        builder.Property(ps => ps.ProficiencyRequired)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SkillProficiency.Intermediate)
            .HasSentinel(SkillProficiency.Intermediate);

        builder.Property(ps => ps.Weight)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(ps => ps.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(ps => ps.ProjectId)
            .HasDatabaseName("IX_ProjectSkills_ProjectId");

        builder.HasIndex(ps => ps.SkillId)
            .HasDatabaseName("IX_ProjectSkills_SkillId");

        builder.HasIndex(ps => ps.ProficiencyRequired)
            .HasDatabaseName("IX_ProjectSkills_ProficiencyRequired");

        builder.HasIndex(ps => ps.Weight)
            .HasDatabaseName("IX_ProjectSkills_Weight");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectSkills_ProficiencyRequired", "[ProficiencyRequired] >= 1 AND [ProficiencyRequired] <= 5"));
        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectSkills_Weight", "[Weight] >= 1 AND [Weight] <= 5"));

        // Relationships
        builder.HasOne(ps => ps.Project)
            .WithMany(p => p.ProjectSkills)
            .HasForeignKey(ps => ps.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Skill)
            .WithMany(s => s.ProjectSkills)
            .HasForeignKey(ps => ps.SkillId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete skills
    }
}