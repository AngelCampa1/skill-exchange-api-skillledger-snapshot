using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class SubscriptionTransactionConfiguration : IEntityTypeConfiguration<SubscriptionTransaction>
{
    public void Configure(EntityTypeBuilder<SubscriptionTransaction> builder)
    {
        // Primary key
        builder.HasKey(st => st.Id);

        // Properties
        builder.Property(st => st.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(st => st.Amount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(st => st.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(st => st.ExternalTransactionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(st => st.ExternalChargeId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(st => st.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(st => st.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(st => st.FailureReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(st => st.RetryCount)
            .IsRequired();

        builder.Property(st => st.NextRetryAt)
            .IsRequired(false);

        builder.Property(st => st.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(st => st.ProcessedAt)
            .IsRequired(false);

        builder.Property(st => st.CompletedAt)
            .IsRequired(false);

        builder.Property(st => st.FailedAt)
            .IsRequired(false);

        builder.Property(st => st.RefundedAt)
            .IsRequired(false);

        builder.Property(st => st.RefundAmount)
            .HasPrecision(10, 2)
            .IsRequired(false);

        builder.Property(st => st.CreatedFromIP)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(st => st.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(st => st.UserId)
            .HasDatabaseName("IX_SubscriptionTransactions_UserId");

        builder.HasIndex(st => st.SubscriptionId)
            .HasDatabaseName("IX_SubscriptionTransactions_SubscriptionId");

        builder.HasIndex(st => st.PaymentMethodId)
            .HasDatabaseName("IX_SubscriptionTransactions_PaymentMethodId");

        builder.HasIndex(st => new { st.UserId, st.CreatedAt })
            .HasDatabaseName("IX_SubscriptionTransactions_UserId_CreatedAt");

        builder.HasIndex(st => new { st.SubscriptionId, st.Type, st.CreatedAt })
            .HasDatabaseName("IX_SubscriptionTransactions_SubscriptionId_Type_CreatedAt");

        builder.HasIndex(st => st.Status)
            .HasDatabaseName("IX_SubscriptionTransactions_Status");

        builder.HasIndex(st => st.NextRetryAt)
            .HasDatabaseName("IX_SubscriptionTransactions_NextRetryAt")
            .HasFilter("[NextRetryAt] IS NOT NULL");

        builder.HasIndex(st => st.ExternalTransactionId)
            .HasDatabaseName("IX_SubscriptionTransactions_ExternalTransactionId")
            .HasFilter("[ExternalTransactionId] IS NOT NULL");

        builder.HasIndex(st => st.ExternalChargeId)
            .HasDatabaseName("IX_SubscriptionTransactions_ExternalChargeId")
            .HasFilter("[ExternalChargeId] IS NOT NULL");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_Amount_Positive",
            "[Amount] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_Currency_Valid",
            "LEN([Currency]) = 3 AND [Currency] LIKE '[A-Z][A-Z][A-Z]'"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_RetryCount_NonNegative",
            "[RetryCount] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_RefundAmount_Positive",
            "[RefundAmount] IS NULL OR [RefundAmount] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_RefundAmount_LessEqual_Amount",
            "[RefundAmount] IS NULL OR [RefundAmount] <= [Amount]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_ProcessedAt_After_CreatedAt",
            "[ProcessedAt] IS NULL OR [CreatedAt] <= [ProcessedAt]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_CompletedAt_After_ProcessedAt",
            "[CompletedAt] IS NULL OR [ProcessedAt] IS NULL OR [ProcessedAt] <= [CompletedAt]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_FailedAt_After_CreatedAt",
            "[FailedAt] IS NULL OR [CreatedAt] <= [FailedAt]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_RefundedAt_After_CompletedAt",
            "[RefundedAt] IS NULL OR [CompletedAt] IS NULL OR [CompletedAt] <= [RefundedAt]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SubscriptionTransactions_NextRetryAt_After_FailedAt",
            "[NextRetryAt] IS NULL OR [FailedAt] IS NULL OR [FailedAt] <= [NextRetryAt]"));

        // Relationships
        builder.HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.Subscription)
            .WithMany(s => s.Transactions)
            .HasForeignKey(st => st.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.PaymentMethod)
            .WithMany()
            .HasForeignKey(st => st.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}