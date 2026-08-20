using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for SavedSearch entity
/// </summary>
public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        // Table configuration
        builder.ToTable("SavedSearches");

        // Primary key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.SearchCriteria)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // Store as JSON

        builder.Property(s => s.NotificationFrequency)
            .HasMaxLength(20)
            .HasDefaultValue("daily");

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UsageCount)
            .HasDefaultValue(0);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_SavedSearches_UserId");

        builder.HasIndex(s => new { s.UserId, s.IsActive })
            .HasDatabaseName("IX_SavedSearches_UserId_IsActive");

        builder.HasIndex(s => s.LastUsedAt)
            .HasDatabaseName("IX_SavedSearches_LastUsedAt");

        builder.HasIndex(s => new { s.NotificationsEnabled, s.IsActive })
            .HasDatabaseName("IX_SavedSearches_Notifications_Active");
    }
}