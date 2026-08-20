using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles");

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.FirstName)
            .HasMaxLength(50);

        builder.Property(p => p.LastName)
            .HasMaxLength(50);

        builder.Property(p => p.Title)
            .HasMaxLength(100);

        builder.Property(p => p.Summary)
            .HasMaxLength(500);

        builder.Property(p => p.Company)
            .HasMaxLength(100);

        builder.Property(p => p.WebsiteUrl)
            .HasMaxLength(255);

        builder.Property(p => p.LinkedInUrl)
            .HasMaxLength(255);

        builder.Property(p => p.GitHubUrl)
            .HasMaxLength(255);

        builder.Property(p => p.Location)
            .HasMaxLength(100);

        builder.Property(p => p.TimeZone)
            .HasMaxLength(50);

        builder.Property(p => p.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(p => p.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.IsComplete)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Profiles_UserId");

        builder.HasIndex(p => p.IsPublic)
            .HasDatabaseName("IX_Profiles_IsPublic");

        builder.HasIndex(p => new { p.FirstName, p.LastName })
            .HasDatabaseName("IX_Profiles_FirstName_LastName");

        builder.HasIndex(p => p.Company)
            .HasDatabaseName("IX_Profiles_Company");

        // Performance index for view count leaderboard queries
        builder.HasIndex(p => new { p.ViewCount, p.UserId })
            .IsDescending(true, false)  // ViewCount DESC, UserId ASC
            .HasDatabaseName("IX_Profiles_ViewCount_UserId");

        // Relationships
        builder.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<Profile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}