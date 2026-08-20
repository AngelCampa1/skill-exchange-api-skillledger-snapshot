using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.ToTable("UserSkills");

        // Primary Key
        builder.HasKey(us => us.Id);

        // Properties
        builder.Property(us => us.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(us => us.UserId)
            .IsRequired();

        builder.Property(us => us.SkillId)
            .IsRequired();

        builder.Property(us => us.Proficiency)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SkillProficiency.Beginner)
            .HasSentinel(SkillProficiency.Beginner);

        builder.Property(us => us.YearsOfExperience)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(us => us.Notes)
            .HasMaxLength(1000);

        builder.Property(us => us.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(us => us.IsVisible)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(us => us.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(us => us.UserId)
            .HasDatabaseName("IX_UserSkills_UserId");

        builder.HasIndex(us => us.SkillId)
            .HasDatabaseName("IX_UserSkills_SkillId");

        builder.HasIndex(us => new { us.UserId, us.SkillId })
            .IsUnique()
            .HasDatabaseName("IX_UserSkills_UserId_SkillId");

        builder.HasIndex(us => new { us.UserId, us.IsFeatured })
            .HasDatabaseName("IX_UserSkills_UserId_IsFeatured");

        builder.HasIndex(us => new { us.UserId, us.IsVisible })
            .HasDatabaseName("IX_UserSkills_UserId_IsVisible");

        // Performance index for skill-based user search queries
        builder.HasIndex(us => new { us.SkillId, us.UserId })
            .HasDatabaseName("IX_UserSkills_SkillId_UserId");

        // Relationships
        builder.HasOne(us => us.User)
            .WithMany(u => u.UserSkills)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(us => us.Skill)
            .WithMany(s => s.UserSkills)
            .HasForeignKey(us => us.SkillId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete skills when users are deleted
    }
}