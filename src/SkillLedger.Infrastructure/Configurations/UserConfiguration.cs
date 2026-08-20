using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Primary key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.Status)
            .HasConversion<int>()
            .HasDefaultValue(UserStatus.Active)
            .IsRequired();


        builder.Property(u => u.TaxCompliant)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(u => u.CreatedFromIP)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(u => u.UpdatedFromIP)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.ExternalCustomerId)
            .HasMaxLength(200)
            .IsRequired(false);

        // Email uniqueness index
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // BUG-039 FIX: Add database-level constraints for data integrity

        // Check constraint: Email must contain @ symbol (basic validation)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Users_Email_Format",
            "[Email] LIKE '%@%.%'"));

        // Check constraint: Failed login attempts must be non-negative
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Users_FailedLoginAttempts_NonNegative",
            "[FailedLoginAttempts] >= 0"));


        // Performance indexes
        builder.HasIndex(u => new { u.Status, u.CreatedAt })
            .HasDatabaseName("IX_Users_Status_CreatedAt");

        builder.HasIndex(u => u.UserName)
            .HasDatabaseName("IX_Users_UserName")
            .HasFilter("[UserName] IS NOT NULL");


        builder.HasMany(u => u.AuditLogs)
            .WithOne(al => al.User)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}