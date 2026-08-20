using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class BadgeDefinitionConfiguration : IEntityTypeConfiguration<BadgeDefinition>
{
    public void Configure(EntityTypeBuilder<BadgeDefinition> builder)
    {
        builder.ToTable("BadgeDefinitions");

        builder.HasKey(bd => bd.Id);

        builder.Property(bd => bd.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(bd => bd.BadgeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bd => bd.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(bd => bd.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bd => bd.Description)
            .HasMaxLength(500);

        builder.Property(bd => bd.IconUrl)
            .HasMaxLength(500);

        builder.Property(bd => bd.RequiredVerification)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(bd => bd.ExpirationPeriod);

        builder.Property(bd => bd.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(bd => bd.DisplayPriority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(bd => bd.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(bd => bd.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasMany(bd => bd.Criteria)
            .WithOne()
            .HasForeignKey(bc => bc.BadgeType)
            .HasPrincipalKey(bd => bd.BadgeType)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(bd => bd.BadgeType)
            .IsUnique()
            .HasDatabaseName("IX_BadgeDefinitions_BadgeType");

        builder.HasIndex(bd => bd.Category)
            .HasDatabaseName("IX_BadgeDefinitions_Category");

        builder.HasIndex(bd => bd.IsActive)
            .HasDatabaseName("IX_BadgeDefinitions_IsActive");

        builder.HasIndex(bd => bd.DisplayPriority)
            .HasDatabaseName("IX_BadgeDefinitions_DisplayPriority");
    }
}