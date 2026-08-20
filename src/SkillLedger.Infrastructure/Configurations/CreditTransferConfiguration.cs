using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for CreditTransfer entity
/// Implements comprehensive database constraints and optimizations for financial operations
/// </summary>
public class CreditTransferConfiguration : IEntityTypeConfiguration<CreditTransfer>
{
    public void Configure(EntityTypeBuilder<CreditTransfer> builder)
    {
        // Table name and schema
        builder.ToTable("CreditTransfers");

        // Primary key
        builder.HasKey(ct => ct.Id);
        builder.Property(ct => ct.Id)
            .ValueGeneratedNever(); // Generated in entity constructor

        // Foreign key relationships
        builder.HasOne(ct => ct.FromUser)
            .WithMany()
            .HasForeignKey(ct => ct.FromUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CreditTransfers_FromUser");

        builder.HasOne(ct => ct.ToUser)
            .WithMany()
            .HasForeignKey(ct => ct.ToUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CreditTransfers_ToUser");

        // Amount constraints
        builder.Property(ct => ct.Amount)
            .IsRequired()
            .HasColumnType("int");

        // Check constraints for business rules
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditTransfers_Amount_Positive",
            "\"Amount\" > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditTransfers_TransferFee_NonNegative",
            "\"TransferFee\" >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditTransfers_NotSelfTransfer",
            "\"FromUserId\" != \"ToUserId\""));

        // Transfer fee
        builder.Property(ct => ct.TransferFee)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnType("int");

        // Message field
        builder.Property(ct => ct.Message)
            .HasMaxLength(500)
            .IsUnicode(true);

        // Status enum
        builder.Property(ct => ct.Status)
            .IsRequired()
            .HasDefaultValue(TransferStatus.Pending)
            .HasConversion<int>();

        // Transaction hash for integrity
        builder.Property(ct => ct.TransactionHash)
            .IsRequired()
            .HasMaxLength(64)
            .IsUnicode(false);

        // Unique constraint on transaction hash
        builder.HasIndex(ct => ct.TransactionHash)
            .IsUnique()
            .HasDatabaseName("IX_CreditTransfers_TransactionHash_Unique");

        // Timestamp fields
        builder.Property(ct => ct.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ct => ct.CompletedAt)
            .IsRequired(false);

        builder.Property(ct => ct.ReversedAt)
            .IsRequired(false);

        builder.Property(ct => ct.ReversalReason)
            .HasMaxLength(500)
            .IsUnicode(true);

        // Audit fields for fraud detection
        builder.Property(ct => ct.InitiatedFromIP)
            .HasMaxLength(45) // IPv6 support
            .IsUnicode(false);

        builder.Property(ct => ct.UserAgent)
            .HasMaxLength(500)
            .IsUnicode(true);

        // Optimistic concurrency
        builder.Property(ct => ct.RowVersion)
            .IsRequired()
            .IsRowVersion();

        // Performance indexes
        builder.HasIndex(ct => new { ct.FromUserId, ct.CreatedAt })
            .HasDatabaseName("IX_CreditTransfers_FromUser_CreatedAt")
            .IncludeProperties(ct => new { ct.Amount, ct.Status });

        builder.HasIndex(ct => new { ct.ToUserId, ct.CreatedAt })
            .HasDatabaseName("IX_CreditTransfers_ToUser_CreatedAt")
            .IncludeProperties(ct => new { ct.Amount, ct.Status });

        builder.HasIndex(ct => ct.Status)
            .HasDatabaseName("IX_CreditTransfers_Status")
            .IncludeProperties(ct => new { ct.CreatedAt, ct.Amount });

        builder.HasIndex(ct => ct.CreatedAt)
            .HasDatabaseName("IX_CreditTransfers_CreatedAt");

        // Fraud detection indexes
        builder.HasIndex(ct => new { ct.InitiatedFromIP, ct.CreatedAt })
            .HasDatabaseName("IX_CreditTransfers_IP_CreatedAt");

        builder.HasIndex(ct => new { ct.FromUserId, ct.Status, ct.CreatedAt })
            .HasDatabaseName("IX_CreditTransfers_FromUser_Status_CreatedAt");

        // Composite index for performance on common queries
        builder.HasIndex(ct => new { ct.Status, ct.CreatedAt, ct.Amount })
            .HasDatabaseName("IX_CreditTransfers_Status_CreatedAt_Amount");

        // Index for reversal eligibility queries
        builder.HasIndex(ct => new { ct.Status, ct.CompletedAt })
            .HasDatabaseName("IX_CreditTransfers_Status_CompletedAt")
            .HasFilter("\"CompletedAt\" IS NOT NULL");

        // Exclude from mapped properties (calculated in entity)
        builder.Ignore(ct => ct.TotalAmount);
        builder.Ignore(ct => ct.IsTerminal);
        builder.Ignore(ct => ct.IsBatchTransfer);

        // Additional database constraints for data integrity
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditTransfers_CompletedAt_Logic",
            "(\"Status\" = 1 AND \"CompletedAt\" IS NOT NULL) OR (\"Status\" != 1)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditTransfers_ReversedAt_Logic",
            "(\"Status\" = 3 AND \"ReversedAt\" IS NOT NULL) OR (\"Status\" != 3)"));

        // Performance optimization: Partition by year for large datasets
        // Note: This would be implemented at deployment time with SQL scripts
        // as EF Core doesn't directly support table partitioning

        // Add comment for documentation
        builder.ToTable(t => t.HasComment("Direct credit transfers between users with comprehensive audit trail and fraud prevention"));
    }
}
