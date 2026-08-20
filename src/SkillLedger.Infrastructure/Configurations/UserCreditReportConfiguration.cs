using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for UserCreditReport entity
/// Defines database schema, constraints, and relationships for pre-aggregated financial reports
/// </summary>
public class UserCreditReportConfiguration : IEntityTypeConfiguration<UserCreditReport>
{
    public void Configure(EntityTypeBuilder<UserCreditReport> builder)
    {
        // Table configuration
        builder.ToTable("UserCreditReports");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Guid will be set by application

        // User relationship
        builder.Property(r => r.UserId)
            .IsRequired();

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("IX_UserCreditReports_UserId");

        // Report identification
        builder.Property(r => r.ReportMonth)
            .IsRequired()
            .HasComment("Report month in YYYYMM format");

        // Unique constraint: one report per user per month
        builder.HasIndex(r => new { r.UserId, r.ReportMonth })
            .IsUnique()
            .HasDatabaseName("IX_UserCreditReports_UserId_ReportMonth");

        // Financial data properties
        builder.Property(r => r.TotalEarned)
            .IsRequired()
            .HasComment("Total credits earned during the month");

        builder.Property(r => r.TotalSpent)
            .IsRequired()
            .HasComment("Total credits spent during the month");

        builder.Property(r => r.TransactionCount)
            .IsRequired()
            .HasComment("Number of transactions during the month");

        builder.Property(r => r.AverageTransactionSize)
            .HasPrecision(18, 2)
            .HasComment("Average transaction amount (calculated field)");

        // Category breakdowns stored as JSON
        builder.Property(r => r.EarningsByType)
            .HasMaxLength(2000)
            .HasComment("JSON data of earnings breakdown by transaction type");

        builder.Property(r => r.SpendingByType)
            .HasMaxLength(2000)
            .HasComment("JSON data of spending breakdown by transaction type");

        builder.Property(r => r.ProjectEarnings)
            .HasMaxLength(2000)
            .HasComment("JSON data of project-related earnings");

        // Timestamps
        builder.Property(r => r.GeneratedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasComment("When the report was generated");

        builder.Property(r => r.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasComment("When the report was last updated");

        // Business rule constraints using check constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserCreditReports_ValidReportMonth",
            "[ReportMonth] >= 190001 AND [ReportMonth] <= 999912"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserCreditReports_ValidTotals",
            "[TotalEarned] >= 0 AND [TotalSpent] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UserCreditReports_ValidTransactionCount",
            "[TransactionCount] >= 0"));

        // Foreign key relationship to Users table
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserCreditReports_Users_UserId")
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(r => r.ReportMonth)
            .HasDatabaseName("IX_UserCreditReports_ReportMonth");

        builder.HasIndex(r => r.GeneratedAt)
            .HasDatabaseName("IX_UserCreditReports_GeneratedAt");

        // Composite index for reporting queries
        builder.HasIndex(r => new { r.UserId, r.GeneratedAt })
            .HasDatabaseName("IX_UserCreditReports_UserId_GeneratedAt");
    }
}