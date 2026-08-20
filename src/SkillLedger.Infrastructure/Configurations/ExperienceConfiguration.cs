using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("Experiences");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ExperienceType.Work)
            .HasSentinel(ExperienceType.Work);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Organization)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Location)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.EndDate);

        builder.Property(e => e.IsCurrent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsVisible)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Experiences_UserId");

        builder.HasIndex(e => new { e.UserId, e.Type })
            .HasDatabaseName("IX_Experiences_UserId_Type");

        builder.HasIndex(e => new { e.UserId, e.StartDate })
            .HasDatabaseName("IX_Experiences_UserId_StartDate");

        builder.HasIndex(e => new { e.UserId, e.IsCurrent })
            .HasDatabaseName("IX_Experiences_UserId_IsCurrent");

        builder.HasIndex(e => new { e.UserId, e.IsVisible })
            .HasDatabaseName("IX_Experiences_UserId_IsVisible");

        builder.HasIndex(e => new { e.UserId, e.DisplayOrder })
            .HasDatabaseName("IX_Experiences_UserId_DisplayOrder");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Experiences)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}