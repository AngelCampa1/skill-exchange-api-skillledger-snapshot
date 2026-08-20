using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class BadgeCriteriaConfiguration : IEntityTypeConfiguration<BadgeCriteria>
{
    public void Configure(EntityTypeBuilder<BadgeCriteria> builder)
    {
        builder.ToTable("BadgeCriteria");

        builder.HasKey(bc => bc.Id);

        builder.Property(bc => bc.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(bc => bc.BadgeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bc => bc.CriteriaName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bc => bc.CriteriaValue)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(bc => bc.CriteriaExpression)
            .HasColumnType("nvarchar(max)");

        builder.Property(bc => bc.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(bc => bc.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(bc => bc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(bc => bc.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(bc => bc.BadgeType)
            .HasDatabaseName("IX_BadgeCriteria_BadgeType");

        builder.HasIndex(bc => bc.IsActive)
            .HasDatabaseName("IX_BadgeCriteria_IsActive");

        builder.HasIndex(bc => new { bc.BadgeType, bc.Priority })
            .HasDatabaseName("IX_BadgeCriteria_BadgeType_Priority");
    }
}