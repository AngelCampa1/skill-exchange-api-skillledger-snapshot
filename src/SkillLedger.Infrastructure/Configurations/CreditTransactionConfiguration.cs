using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for CreditTransaction entity
/// Implements immutable ledger requirements and cryptographic integrity
/// </summary>
public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.ToTable("CreditTransactions");

        // Primary key
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
               .HasDefaultValueSql("gen_random_uuid()");

        // Amount constraint - must be positive
        builder.Property(t => t.Amount)
               .IsRequired();

        // Add check constraint for positive amounts
        builder.ToTable(t => t.HasCheckConstraint("CK_CreditTransactions_Amount_Positive", "Amount > 0"));

        // Transaction type enum
        builder.Property(t => t.Type)
               .HasConversion<int>()
               .IsRequired();

        // Transaction status enum
        builder.Property(t => t.Status)
               .HasConversion<int>()
               .HasDefaultValue(TransactionStatus.Pending);

        // Description is required for audit trail
        builder.Property(t => t.Description)
               .IsRequired()
               .HasMaxLength(500);

        // Cryptographic hash for tamper detection (required and unique)
        builder.Property(t => t.TransactionHash)
               .IsRequired()
               .HasMaxLength(128);

        builder.HasIndex(t => t.TransactionHash)
               .IsUnique()
               .HasDatabaseName("IX_CreditTransactions_Hash_Unique");

        // Previous transaction hash for blockchain-inspired chain integrity
        builder.Property(t => t.PreviousTransactionHash)
               .HasMaxLength(128);

        // Audit trail fields
        builder.Property(t => t.CreatedAt)
               .HasDefaultValueSql("NOW()");

        builder.Property(t => t.FailureReason)
               .HasMaxLength(500);

        builder.Property(t => t.InitiatedFromIP)
               .HasMaxLength(45); // IPv6 max length

        builder.Property(t => t.UserAgent)
               .HasMaxLength(500);

        builder.Property(t => t.Metadata)
               .HasMaxLength(2000);

        builder.Property(t => t.FlaggedReason)
               .HasMaxLength(500);

        // User relationships
        builder.HasOne(t => t.FromUser)
               .WithMany()
               .HasForeignKey(t => t.FromUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToUser)
               .WithMany()
               .HasForeignKey(t => t.ToUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        // Project relationship (optional)
        builder.HasOne(t => t.Project)
               .WithMany()
               .HasForeignKey(t => t.ProjectId)
               .OnDelete(DeleteBehavior.SetNull);

        // Performance indexes for common queries
        builder.HasIndex(t => new { t.FromUserId, t.CreatedAt })
               .HasDatabaseName("IX_CreditTransactions_FromUser_Created");

        builder.HasIndex(t => new { t.ToUserId, t.CreatedAt })
               .HasDatabaseName("IX_CreditTransactions_ToUser_Created");

        builder.HasIndex(t => new { t.ProjectId, t.Type })
               .HasDatabaseName("IX_CreditTransactions_Project_Type");

        builder.HasIndex(t => t.Status)
               .HasDatabaseName("IX_CreditTransactions_Status");

        builder.HasIndex(t => t.Type)
               .HasDatabaseName("IX_CreditTransactions_Type");

        // Security and fraud detection indexes
        builder.HasIndex(t => t.IsFlagged)
               .HasDatabaseName("IX_CreditTransactions_IsFlagged");

        builder.HasIndex(t => new { t.InitiatedFromIP, t.CreatedAt })
               .HasDatabaseName("IX_CreditTransactions_IP_Created");

        builder.HasIndex(t => new { t.Amount, t.CreatedAt })
               .HasDatabaseName("IX_CreditTransactions_Amount_Created");

        // Audit trail index for transaction chain verification
        builder.HasIndex(t => new { t.CreatedAt, t.PreviousTransactionHash })
               .HasDatabaseName("IX_CreditTransactions_Chain_Integrity");

        // Performance index for user transaction history queries
        builder.HasIndex(t => new { t.FromUserId, t.ToUserId, t.CreatedAt })
               .HasDatabaseName("IX_CreditTransactions_Users_Created");

        // Index for escrow operations
        builder.HasIndex(t => new { t.ProjectId, t.Type, t.Status })
               .HasDatabaseName("IX_CreditTransactions_Escrow_Operations");

        // Add constraints to ensure data integrity

        // At least one user must be specified (FromUserId OR ToUserId must be not null)
        // This is handled at the application level since EF Core doesn't support OR constraints well

        // Ensure completion timestamp is set when status is completed
        // This is also handled at the application level

        // Add indexes for reporting and analytics
        builder.HasIndex(t => new { t.Type, t.Status, t.CreatedAt })
               .HasDatabaseName("IX_CreditTransactions_Reporting");

        builder.HasIndex(t => new { t.Status, t.CompletedAt })
               .HasDatabaseName("IX_CreditTransactions_Completion");
    }
}
