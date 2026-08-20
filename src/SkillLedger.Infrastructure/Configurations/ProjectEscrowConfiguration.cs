using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectEscrowConfiguration : IEntityTypeConfiguration<ProjectEscrow>
{
    public void Configure(EntityTypeBuilder<ProjectEscrow> builder)
    {
        builder.ToTable("ProjectEscrows");

        // Primary key
        builder.HasKey(e => e.Id);

        // Project relationship (one-to-one)
        builder.HasOne(e => e.Project)
               .WithMany()
               .HasForeignKey(e => e.ProjectId)
               .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - one escrow per project
        builder.HasIndex(e => e.ProjectId)
               .IsUnique()
               .HasDatabaseName("IX_ProjectEscrows_ProjectId_Unique");

        // Client relationship
        builder.HasOne(e => e.Client)
               .WithMany()
               .HasForeignKey(e => e.ClientId)
               .OnDelete(DeleteBehavior.Restrict);

        // Provider relationship
        builder.HasOne(e => e.Provider)
               .WithMany()
               .HasForeignKey(e => e.ProviderId)
               .OnDelete(DeleteBehavior.Restrict);

        // Dispute resolution admin relationship
        builder.HasOne(e => e.DisputeResolvedByUser)
               .WithMany()
               .HasForeignKey(e => e.DisputeResolvedByUserId)
               .OnDelete(DeleteBehavior.SetNull);

        // Property configurations
        builder.Property(e => e.TotalAmount)
               .IsRequired()
               .HasComment("Total amount of credits in escrow");

        builder.Property(e => e.ReleasedAmount)
               .HasDefaultValue(0)
               .HasComment("Amount released to provider so far");

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>()
               .HasComment("Current status of escrow account");

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("NOW()")
               .HasComment("When escrow account was created");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasDefaultValueSql("NOW()")
               .HasComment("When escrow was last updated");

        builder.Property(e => e.CreatedFromIP)
               .HasMaxLength(45)
               .HasComment("IP address where escrow was created");

        builder.Property(e => e.Notes)
               .HasMaxLength(1000)
               .HasComment("Optional notes about the escrow");

        builder.Property(e => e.DisputeReason)
               .HasMaxLength(1000)
               .HasComment("Reason for dispute if status is Disputed");

        builder.Property(e => e.DisputeResolutionNotes)
               .HasMaxLength(1000)
               .HasComment("Admin notes for dispute resolution");

        builder.Property(e => e.RequiresMultiSignature)
               .HasDefaultValue(false)
               .HasComment("Whether escrow requires multi-signature approval");

        // Indexes for performance
        builder.HasIndex(e => e.ClientId)
               .HasDatabaseName("IX_ProjectEscrows_ClientId");

        builder.HasIndex(e => e.ProviderId)
               .HasDatabaseName("IX_ProjectEscrows_ProviderId");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_ProjectEscrows_Status");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_ProjectEscrows_CreatedAt");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
               .HasDatabaseName("IX_ProjectEscrows_Status_CreatedAt");

        // Check constraints for business rules
        builder.ToTable(table => table.HasCheckConstraint("CK_ProjectEscrows_TotalAmount_Positive", "[TotalAmount] > 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_ProjectEscrows_ReleasedAmount_NonNegative", "[ReleasedAmount] >= 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_ProjectEscrows_ReleasedAmount_LTE_TotalAmount", "[ReleasedAmount] <= [TotalAmount]"));

        // Computed column for remaining amount (not mapped to entity as it's a computed property)
        // builder.Property<int>("RemainingAmount")
        //        .HasComputedColumnSql("[TotalAmount] - [ReleasedAmount]", stored: true);

        // Audit trail
        builder.HasMany(e => e.AuditLogs)
               .WithOne()
               .HasForeignKey("EntityId")
               .HasPrincipalKey(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade);
    }
}