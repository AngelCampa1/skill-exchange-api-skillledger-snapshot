using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class ProviderSelectionConfiguration : IEntityTypeConfiguration<ProviderSelection>
{
    public void Configure(EntityTypeBuilder<ProviderSelection> builder)
    {
        builder.ToTable("ProviderSelections");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWID()");

        // Required properties
        builder.Property(e => e.ProjectId)
            .IsRequired();

        builder.Property(e => e.SelectedProviderId)
            .IsRequired();

        builder.Property(e => e.SelectedApplicationId)
            .IsRequired();

        builder.Property(e => e.SelectionReason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.EscrowAmount)
            .IsRequired();

        builder.Property(e => e.SelectedAt)
            .HasDefaultValueSql("NOW()");

        // Optional properties
        builder.Property(e => e.ContractTerms)
            .HasMaxLength(5000);

        builder.Property(e => e.NegotiationNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.SelectedFromIP)
            .HasMaxLength(45);

        // Enum property
        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ProviderSelectionStatus.Selected);

        // Boolean properties with defaults
        builder.Property(e => e.IsEscrowFunded)
            .HasDefaultValue(false);

        builder.Property(e => e.IsContractSigned)
            .HasDefaultValue(false);

        // Foreign key relationships
        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SelectedProvider)
            .WithMany()
            .HasForeignKey(e => e.SelectedProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SelectedApplication)
            .WithMany()
            .HasForeignKey(e => e.SelectedApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - one selection per project
        builder.HasIndex(e => e.ProjectId)
            .IsUnique()
            .HasDatabaseName("IX_ProviderSelections_ProjectId_Unique");

        // Index on provider for performance
        builder.HasIndex(e => e.SelectedProviderId)
            .HasDatabaseName("IX_ProviderSelections_SelectedProviderId");

        // Index on status for filtering
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_ProviderSelections_Status");

        // Composite index for selection date queries
        builder.HasIndex(e => new { e.SelectedAt, e.Status })
            .HasDatabaseName("IX_ProviderSelections_SelectedAt_Status");
    }
}