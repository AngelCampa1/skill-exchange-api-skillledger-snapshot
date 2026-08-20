using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectDeliverableConfiguration : IEntityTypeConfiguration<ProjectDeliverable>
{
    public void Configure(EntityTypeBuilder<ProjectDeliverable> builder)
    {
        builder.ToTable("ProjectDeliverables");

        // Primary Key
        builder.HasKey(pd => pd.Id);

        // Properties
        builder.Property(pd => pd.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the entity constructor

        builder.Property(pd => pd.ProjectId)
            .IsRequired();

        builder.Property(pd => pd.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pd => pd.OrderIndex)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pd => pd.IsRequired)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pd => pd.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pd => pd.CompletedAt);

        builder.Property(pd => pd.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(pd => pd.ProjectId)
            .HasDatabaseName("IX_ProjectDeliverables_ProjectId");

        builder.HasIndex(pd => new { pd.ProjectId, pd.OrderIndex })
            .HasDatabaseName("IX_ProjectDeliverables_ProjectId_OrderIndex");

        builder.HasIndex(pd => pd.IsCompleted)
            .HasDatabaseName("IX_ProjectDeliverables_IsCompleted");

        // Check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_ProjectDeliverables_OrderIndex", "[OrderIndex] >= 0 AND [OrderIndex] <= 100"));

        // Relationships
        builder.HasOne(pd => pd.Project)
            .WithMany(p => p.Deliverables)
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}