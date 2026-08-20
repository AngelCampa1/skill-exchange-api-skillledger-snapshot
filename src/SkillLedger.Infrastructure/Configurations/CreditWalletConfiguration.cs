using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for CreditWallet entity
/// Implements encrypted storage requirements and security constraints
/// </summary>
public class CreditWalletConfiguration : IEntityTypeConfiguration<CreditWallet>
{
    public void Configure(EntityTypeBuilder<CreditWallet> builder)
    {
        builder.ToTable("CreditWallets");

        // Primary key
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
               .HasDefaultValueSql("NEWID()");

        // User relationship - one-to-one
        builder.HasIndex(w => w.UserId)
               .IsUnique()
               .HasDatabaseName("IX_CreditWallets_UserId_Unique");

        builder.HasOne(w => w.User)
               .WithMany()
               .HasForeignKey(w => w.UserId)
               .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Encrypted financial data fields
        builder.Property(w => w.EncryptedBalance)
               .IsRequired()
               .HasMaxLength(512)
               .HasColumnType("NVARCHAR(512)");

        builder.Property(w => w.EncryptedPendingBalance)
               .IsRequired()
               .HasMaxLength(512)
               .HasColumnType("NVARCHAR(512)");

        builder.Property(w => w.EncryptedTotalEarned)
               .IsRequired()
               .HasMaxLength(512)
               .HasColumnType("NVARCHAR(512)");

        builder.Property(w => w.EncryptedTotalSpent)
               .IsRequired()
               .HasMaxLength(512)
               .HasColumnType("NVARCHAR(512)");

        // Metadata and audit fields
        builder.Property(w => w.KeyIdentifier)
               .IsRequired()
               .HasMaxLength(128);

        builder.Property(w => w.BlockedReason)
               .HasMaxLength(500);

        builder.Property(w => w.CreatedAt)
               .HasDefaultValueSql("NOW()");

        builder.Property(w => w.UpdatedAt)
               .HasDefaultValueSql("NOW()");

        // Concurrency control
        builder.Property(w => w.RowVersion)
               .IsRowVersion()
               .HasColumnName("RowVersion");

        // Security indexes
        builder.HasIndex(w => w.IsBlocked)
               .HasDatabaseName("IX_CreditWallets_IsBlocked");

        builder.HasIndex(w => w.LastTransactionAt)
               .HasDatabaseName("IX_CreditWallets_LastTransactionAt");

        // Note: Transaction navigation properties handled at service layer
        // due to nullable foreign key complexity

        // Ignore non-mapped computed properties
        builder.Ignore(w => w.Balance);
        builder.Ignore(w => w.PendingBalance);
        builder.Ignore(w => w.TotalEarned);
        builder.Ignore(w => w.TotalSpent);
        builder.Ignore(w => w.AvailableBalance);

        // Audit logging configuration
        builder.HasIndex(w => new { w.UserId, w.CreatedAt })
               .HasDatabaseName("IX_CreditWallets_User_Created");

        // BUG-039 FIX: Add database-level constraints for data integrity

        // Check constraint: Encrypted fields must not be empty
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditWallets_EncryptedBalance_NotEmpty",
            "LEN([EncryptedBalance]) > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditWallets_EncryptedPendingBalance_NotEmpty",
            "LEN([EncryptedPendingBalance]) > 0"));

        // Check constraint: If wallet is blocked, must have a reason
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditWallets_BlockedReason_Logic",
            "([IsBlocked] = 0) OR ([IsBlocked] = 1 AND [BlockedReason] IS NOT NULL)"));

        // Check constraint: UpdatedAt must be >= CreatedAt
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditWallets_UpdatedAt_Logic",
            "[UpdatedAt] >= [CreatedAt]"));
    }
}