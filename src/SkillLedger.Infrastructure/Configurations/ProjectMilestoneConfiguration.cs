using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for ProjectMilestone entity
/// </summary>
public class ProjectMilestoneConfiguration : IEntityTypeConfiguration<ProjectMilestone>
{
    public void Configure(EntityTypeBuilder<ProjectMilestone> builder)
    {
        // Table configuration
        builder.ToTable("ProjectMilestones");

        // Primary key
        builder.HasKey(pm => pm.Id);
        builder.Property(pm => pm.Id)
            .ValueGeneratedNever(); // We generate GUIDs in constructor

        // Required string properties
        builder.Property(pm => pm.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pm => pm.Description)
            .IsRequired()
            .HasMaxLength(2000);

        // Optional string properties
        builder.Property(pm => pm.AcceptanceCriteria)
            .HasMaxLength(3000);

        builder.Property(pm => pm.ReviewNotes)
            .HasMaxLength(2000);

        builder.Property(pm => pm.CreatedFromIP)
            .HasMaxLength(45);

        // Enum properties
        builder.Property(pm => pm.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(pm => pm.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Decimal properties with precision
        builder.Property(pm => pm.WeightPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        // DateTime properties
        builder.Property(pm => pm.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(pm => pm.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Foreign key relationships
        builder.HasOne(pm => pm.Project)
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.EscrowMilestone)
            .WithOne()
            .HasForeignKey<ProjectMilestone>(pm => pm.EscrowMilestoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pm => pm.CreatedByUser)
            .WithMany()
            .HasForeignKey(pm => pm.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pm => pm.AssignedToUser)
            .WithMany()
            .HasForeignKey(pm => pm.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Collection relationships
        builder.HasMany(pm => pm.Submissions)
            .WithOne(s => s.Milestone)
            .HasForeignKey(s => s.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(pm => pm.ProjectId)
            .HasDatabaseName("IX_ProjectMilestones_ProjectId");

        builder.HasIndex(pm => pm.Status)
            .HasDatabaseName("IX_ProjectMilestones_Status");

        builder.HasIndex(pm => pm.DueDate)
            .HasDatabaseName("IX_ProjectMilestones_DueDate");

        builder.HasIndex(pm => pm.AssignedToUserId)
            .HasDatabaseName("IX_ProjectMilestones_AssignedToUserId");

        builder.HasIndex(pm => new { pm.ProjectId, pm.SequenceOrder })
            .IsUnique()
            .HasDatabaseName("IX_ProjectMilestones_ProjectId_SequenceOrder");

        builder.HasIndex(pm => pm.CreatedAt)
            .HasDatabaseName("IX_ProjectMilestones_CreatedAt");

        // Check constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_ProjectMilestones_WeightPercentage",
            "[WeightPercentage] >= 0 AND [WeightPercentage] <= 100"));
    }
}