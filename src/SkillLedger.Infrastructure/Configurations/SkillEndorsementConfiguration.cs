using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class SkillEndorsementConfiguration : IEntityTypeConfiguration<SkillEndorsement>
{
    public void Configure(EntityTypeBuilder<SkillEndorsement> builder)
    {
        builder.ToTable("SkillEndorsements");

        // Primary Key
        builder.HasKey(se => se.Id);

        // Properties
        builder.Property(se => se.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(se => se.UserSkillId)
            .IsRequired();

        builder.Property(se => se.EndorsedByUserId)
            .IsRequired();

        builder.Property(se => se.Comment)
            .HasMaxLength(500);

        builder.Property(se => se.IsVisible)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(se => se.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(se => se.UserSkillId)
            .HasDatabaseName("IX_SkillEndorsements_UserSkillId");

        builder.HasIndex(se => se.EndorsedByUserId)
            .HasDatabaseName("IX_SkillEndorsements_EndorsedByUserId");

        builder.HasIndex(se => new { se.UserSkillId, se.EndorsedByUserId })
            .IsUnique()
            .HasDatabaseName("IX_SkillEndorsements_UserSkillId_EndorsedByUserId");

        builder.HasIndex(se => new { se.UserSkillId, se.IsVisible })
            .HasDatabaseName("IX_SkillEndorsements_UserSkillId_IsVisible");

        // Relationships
        builder.HasOne(se => se.UserSkill)
            .WithMany(us => us.Endorsements)
            .HasForeignKey(se => se.UserSkillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(se => se.EndorsedByUser)
            .WithMany(u => u.GivenEndorsements)
            .HasForeignKey(se => se.EndorsedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete endorsements when endorsing user is deleted
    }
}