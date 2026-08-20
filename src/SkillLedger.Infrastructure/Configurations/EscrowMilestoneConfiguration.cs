using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class EscrowMilestoneConfiguration : IEntityTypeConfiguration<EscrowMilestone>
{
    public void Configure(EntityTypeBuilder<EscrowMilestone> builder)
    {
        builder.ToTable("EscrowMilestones");

        // Primary key
        builder.HasKey(e => e.Id);

        // Escrow relationship (many-to-one)
        builder.HasOne(e => e.Escrow)
               .WithMany(e => e.Milestones)
               .HasForeignKey(e => e.EscrowId)
               .OnDelete(DeleteBehavior.Cascade);

        // Released by user relationship
        builder.HasOne(e => e.ReleasedByUser)
               .WithMany()
               .HasForeignKey(e => e.ReleasedByUserId)
               .OnDelete(DeleteBehavior.SetNull);

        // Linked deliverable relationship (optional)
        builder.HasOne(e => e.LinkedDeliverable)
               .WithMany()
               .HasForeignKey(e => e.LinkedDeliverableId)
               .OnDelete(DeleteBehavior.SetNull);

        // Property configurations
        builder.Property(e => e.Description)
               .IsRequired()
               .HasMaxLength(500)
               .HasComment("Human-readable milestone description");

        builder.Property(e => e.Amount)
               .IsRequired()
               .HasComment("Credits to release for this milestone");

        builder.Property(e => e.IsReleased)
               .HasDefaultValue(false)
               .HasComment("Whether milestone has been released");

        builder.Property(e => e.SequenceOrder)
               .HasDefaultValue(1)
               .HasComment("Display order for milestones");

        builder.Property(e => e.IsBlocking)
               .HasDefaultValue(false)
               .HasComment("Whether milestone blocks subsequent releases");

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("NOW()")
               .HasComment("When milestone was created");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasDefaultValueSql("NOW()")
               .HasComment("When milestone was last updated");

        builder.Property(e => e.CreatedFromIP)
               .HasMaxLength(45)
               .HasComment("IP address where milestone was created");

        builder.Property(e => e.ReleaseNotes)
               .HasMaxLength(1000)
               .HasComment("Notes about milestone release");

        // Indexes for performance
        builder.HasIndex(e => e.EscrowId)
               .HasDatabaseName("IX_EscrowMilestones_EscrowId");

        builder.HasIndex(e => e.IsReleased)
               .HasDatabaseName("IX_EscrowMilestones_IsReleased");

        builder.HasIndex(e => new { e.EscrowId, e.SequenceOrder })
               .HasDatabaseName("IX_EscrowMilestones_EscrowId_SequenceOrder");

        builder.HasIndex(e => new { e.EscrowId, e.IsReleased })
               .HasDatabaseName("IX_EscrowMilestones_EscrowId_IsReleased");

        builder.HasIndex(e => e.ExpectedCompletionDate)
               .HasDatabaseName("IX_EscrowMilestones_ExpectedCompletionDate")
               .HasFilter("[ExpectedCompletionDate] IS NOT NULL");

        builder.HasIndex(e => e.LinkedDeliverableId)
               .HasDatabaseName("IX_EscrowMilestones_LinkedDeliverableId")
               .HasFilter("[LinkedDeliverableId] IS NOT NULL");

        // Check constraints for business rules
        builder.ToTable(table => table.HasCheckConstraint("CK_EscrowMilestones_Amount_Positive", "[Amount] > 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_EscrowMilestones_SequenceOrder_Positive", "[SequenceOrder] > 0"));

        // Ensure that ActualCompletionDate is not before CreatedAt
        builder.ToTable(table => table.HasCheckConstraint("CK_EscrowMilestones_ActualCompletion_After_Created",
            "[ActualCompletionDate] IS NULL OR [ActualCompletionDate] >= [CreatedAt]"));

        // Audit trail
        builder.HasMany(e => e.AuditLogs)
               .WithOne()
               .HasForeignKey("EntityId")
               .HasPrincipalKey(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade);
    }
}