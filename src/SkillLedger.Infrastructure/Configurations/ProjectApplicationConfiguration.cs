using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectApplicationConfiguration : IEntityTypeConfiguration<ProjectApplication>
{
    public void Configure(EntityTypeBuilder<ProjectApplication> builder)
    {
        builder.ToTable("ProjectApplications");

        // Primary key
        builder.HasKey(pa => pa.Id);

        // Properties
        builder.Property(pa => pa.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(pa => pa.CoverLetter)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(pa => pa.ProposedTimeline)
            .IsRequired(false);

        builder.Property(pa => pa.SkillMatchScore)
            .IsRequired(false)
            .HasColumnType("decimal(3,2)"); // 0.00 to 1.00

        builder.Property(pa => pa.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ApplicationStatus.Pending);

        builder.Property(pa => pa.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(pa => pa.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(pa => pa.ReviewedAt)
            .IsRequired(false);

        builder.Property(pa => pa.ClientFeedback)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(pa => pa.SubmittedFromIP)
            .IsRequired(false)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(pa => pa.IsAvailableImmediately)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pa => pa.ProposedBudget)
            .IsRequired(false);

        // Relationships
        builder.HasOne(pa => pa.Project)
            .WithMany()
            .HasForeignKey(pa => pa.ProjectId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion

        builder.HasOne(pa => pa.Provider)
            .WithMany()
            .HasForeignKey(pa => pa.ProviderId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion

        builder.HasMany(pa => pa.Attachments)
            .WithOne(att => att.ProjectApplication)
            .HasForeignKey(att => att.ProjectApplicationId)
            .OnDelete(DeleteBehavior.Cascade); // Delete attachments when application is deleted

        // Indexes
        builder.HasIndex(pa => pa.ProjectId)
            .HasDatabaseName("IX_ProjectApplications_ProjectId");

        builder.HasIndex(pa => pa.ProviderId)
            .HasDatabaseName("IX_ProjectApplications_ProviderId");

        builder.HasIndex(pa => pa.Status)
            .HasDatabaseName("IX_ProjectApplications_Status");

        builder.HasIndex(pa => pa.CreatedAt)
            .HasDatabaseName("IX_ProjectApplications_CreatedAt");

        builder.HasIndex(pa => pa.SkillMatchScore)
            .HasDatabaseName("IX_ProjectApplications_SkillMatchScore");

        // Composite index for performance
        builder.HasIndex(pa => new { pa.ProjectId, pa.Status, pa.CreatedAt })
            .HasDatabaseName("IX_ProjectApplications_ProjectId_Status_CreatedAt");

        builder.HasIndex(pa => new { pa.ProviderId, pa.Status, pa.CreatedAt })
            .HasDatabaseName("IX_ProjectApplications_ProviderId_Status_CreatedAt");

        // Unique constraint: One application per project per provider
        builder.HasIndex(pa => new { pa.ProjectId, pa.ProviderId })
            .IsUnique()
            .HasDatabaseName("UX_ProjectApplications_ProjectId_ProviderId");
    }
}