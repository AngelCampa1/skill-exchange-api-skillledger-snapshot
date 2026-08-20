using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        // Primary key
        builder.HasKey(pm => pm.Id);

        // Properties
        builder.Property(pm => pm.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pm => pm.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pm => pm.Token)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pm => pm.Last4Digits)
            .HasMaxLength(4)
            .IsRequired(false);

        builder.Property(pm => pm.Brand)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(pm => pm.ExpiryDate)
            .HasMaxLength(7)
            .IsRequired(false);

        builder.Property(pm => pm.CardholderName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(pm => pm.BillingCountry)
            .HasMaxLength(2)
            .IsRequired(false);

        builder.Property(pm => pm.BillingPostalCode)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(pm => pm.IsDefault)
            .IsRequired();

        builder.Property(pm => pm.IsValid)
            .IsRequired();

        builder.Property(pm => pm.ExpiresAt)
            .IsRequired(false);

        builder.Property(pm => pm.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(pm => pm.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(pm => pm.LastUsedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(pm => pm.UserId)
            .HasDatabaseName("IX_PaymentMethods_UserId");

        builder.HasIndex(pm => new { pm.UserId, pm.IsDefault })
            .HasDatabaseName("IX_PaymentMethods_UserId_IsDefault");

        builder.HasIndex(pm => new { pm.UserId, pm.IsValid })
            .HasDatabaseName("IX_PaymentMethods_UserId_IsValid");

        builder.HasIndex(pm => pm.ExpiresAt)
            .HasDatabaseName("IX_PaymentMethods_ExpiresAt")
            .HasFilter("[ExpiresAt] IS NOT NULL");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PaymentMethods_Provider_NotEmpty",
            "LEN([Provider]) > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PaymentMethods_Type_NotEmpty",
            "LEN([Type]) > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PaymentMethods_Token_NotEmpty",
            "LEN([Token]) > 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PaymentMethods_Last4Digits_DigitsOnly",
            "[Last4Digits] IS NULL OR LEN([Last4Digits]) = 4 AND [Last4Digits] LIKE '[0-9][0-9][0-9][0-9]'"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PaymentMethods_ExpiryDate_ValidFormat",
            "[ExpiryDate] IS NULL OR ([ExpiryDate] LIKE '[0-9][0-9]/[0-9][0-9][0-9][0-9]' AND LEN([ExpiryDate]) = 7)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PaymentMethods_BillingCountry_ValidFormat",
            "[BillingCountry] IS NULL OR LEN([BillingCountry]) = 2 AND [BillingCountry] LIKE '[A-Z][A-Z]'"));

        // Unique constraint: One default payment method per user
        builder.HasIndex(pm => new { pm.UserId, pm.IsDefault })
            .HasDatabaseName("IX_PaymentMethods_UserId_DefaultUnique")
            .HasFilter("[IsDefault] = 1")
            .IsUnique();

        // Relationships
        builder.HasOne(pm => pm.User)
            .WithMany(u => u.PaymentMethods)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}