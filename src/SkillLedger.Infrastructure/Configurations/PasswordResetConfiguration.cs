using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for PasswordReset entity
/// </summary>
public class PasswordResetConfiguration : IEntityTypeConfiguration<PasswordReset>
{
    public void Configure(EntityTypeBuilder<PasswordReset> builder)
    {
        // Table configuration
        builder.ToTable("PasswordResets");

        // Primary key
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Id).ValueGeneratedOnAdd();

        // User relationship
        builder.HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Token properties
        builder.Property(pr => pr.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(pr => pr.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        // Timestamp properties
        builder.Property(pr => pr.CreatedAt)
            .IsRequired();

        builder.Property(pr => pr.ExpiresAt)
            .IsRequired();

        builder.Property(pr => pr.UsedAt)
            .IsRequired(false);

        builder.Property(pr => pr.LastAttemptAt)
            .IsRequired(false);

        // Usage tracking
        builder.Property(pr => pr.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pr => pr.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Client information
        builder.Property(pr => pr.IpAddress)
            .IsRequired(false)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(pr => pr.UserAgent)
            .IsRequired(false)
            .HasMaxLength(1000);

        // Indexes for performance and security
        builder.HasIndex(pr => pr.TokenHash)
            .HasDatabaseName("IX_PasswordResets_TokenHash")
            .IsUnique();

        builder.HasIndex(pr => pr.UserId)
            .HasDatabaseName("IX_PasswordResets_UserId");

        builder.HasIndex(pr => pr.ExpiresAt)
            .HasDatabaseName("IX_PasswordResets_ExpiresAt");

        builder.HasIndex(pr => pr.CreatedAt)
            .HasDatabaseName("IX_PasswordResets_CreatedAt");

        builder.HasIndex(pr => new { pr.UserId, pr.CreatedAt })
            .HasDatabaseName("IX_PasswordResets_User_Created");
    }
}