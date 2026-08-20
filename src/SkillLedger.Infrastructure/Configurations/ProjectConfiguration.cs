using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(p => p.ClientId)
            .IsRequired();

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ProjectStatus.Draft);

        builder.Property(p => p.CreditBudget)
            .IsRequired()
            .HasDefaultValue(50);

        builder.Property(p => p.StartDate);

        builder.Property(p => p.EndDate);

        builder.Property(p => p.ModerationStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ModerationStatus.Pending);

        builder.Property(p => p.ModerationNotes)
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.CreatedFromIP)
            .HasMaxLength(45); // IPv6 max length

        // Computed Columns (read-only properties in entity)
        builder.Ignore(p => p.HasValidTimeline);
        builder.Ignore(p => p.IsEditable);
        builder.Ignore(p => p.CanBePublished);

        // Indexes
        builder.HasIndex(p => p.ClientId)
            .HasDatabaseName("IX_Projects_ClientId");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Projects_Status");

        builder.HasIndex(p => p.ModerationStatus)
            .HasDatabaseName("IX_Projects_ModerationStatus");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Projects_CreatedAt");

        builder.HasIndex(p => p.EndDate)
            .HasDatabaseName("IX_Projects_EndDate");

        builder.HasIndex(p => p.CreditBudget)
            .HasDatabaseName("IX_Projects_CreditBudget");

        builder.HasIndex(p => new { p.Status, p.ModerationStatus })
            .HasDatabaseName("IX_Projects_Status_ModerationStatus");

        // Full-text search index for Title and Description (SQL Server specific)
        // This can be added in a migration or through raw SQL

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_Projects_CreditBudget", "[CreditBudget] >= 50 AND [CreditBudget] <= 50000")); // BUG-026 already fixed limit
        builder.ToTable(t => t.HasCheckConstraint("CK_Projects_Timeline", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] > [StartDate]"));

        // BUG-039 FIX: Add additional database-level constraints

        // Check constraint: Title must not be empty or whitespace
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Projects_Title_NotEmpty",
            "LEN(LTRIM(RTRIM([Title]))) > 0"));

        // Check constraint: Description must not be empty or whitespace
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Projects_Description_NotEmpty",
            "LEN(LTRIM(RTRIM([Description]))) > 0"));

        // Check constraint: UpdatedAt must be >= CreatedAt
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Projects_UpdatedAt_Logic",
            "[UpdatedAt] >= [CreatedAt]"));

        // Check constraint: If moderation failed, must have notes
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Projects_ModerationNotes_Logic",
            "([ModerationStatus] != 3) OR ([ModerationStatus] = 3 AND [ModerationNotes] IS NOT NULL)"));

        // Relationships
        builder.HasOne(p => p.Client)
            .WithMany(u => u.ClientProjects)
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Deliverables)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProjectSkills)
            .WithOne(ps => ps.Project)
            .HasForeignKey(ps => ps.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.AuditLogs)
            .WithOne()
            .HasForeignKey("ProjectId")
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete audit logs
    }
}