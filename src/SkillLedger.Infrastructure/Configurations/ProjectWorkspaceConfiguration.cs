using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations
{
    public class ProjectWorkspaceConfiguration : IEntityTypeConfiguration<ProjectWorkspace>
    {
        public void Configure(EntityTypeBuilder<ProjectWorkspace> builder)
        {
            builder.HasKey(pw => pw.Id);

            builder.Property(pw => pw.Id)
                .ValueGeneratedOnAdd();

            builder.Property(pw => pw.ProjectId)
                .IsRequired();

            builder.Property(pw => pw.ClientId)
                .IsRequired();

            builder.Property(pw => pw.ProviderId)
                .IsRequired();

            builder.Property(pw => pw.WorkspaceKey)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(pw => pw.Status)
                .IsRequired()
                .HasDefaultValue(Core.Enums.WorkspaceStatus.Active)
                .HasSentinel(Core.Enums.WorkspaceStatus.Active);

            builder.Property(pw => pw.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(pw => pw.ArchivedAt)
                .IsRequired(false);

            builder.Property(pw => pw.TimelineData)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(pw => pw.MilestoneData)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(pw => pw.LastSyncedAt)
                .IsRequired(false);

            builder.Property(pw => pw.IntegrationStatus)
                .HasMaxLength(100)
                .IsRequired(false);

            // Relationships
            builder.HasOne(pw => pw.Project)
                .WithOne()
                .HasForeignKey<ProjectWorkspace>(pw => pw.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pw => pw.Client)
                .WithMany()
                .HasForeignKey(pw => pw.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pw => pw.Provider)
                .WithMany()
                .HasForeignKey(pw => pw.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.HasIndex(pw => pw.ProjectId)
                .IsUnique()
                .HasDatabaseName("IX_ProjectWorkspaces_ProjectId");

            builder.HasIndex(pw => pw.ClientId)
                .HasDatabaseName("IX_ProjectWorkspaces_ClientId");

            builder.HasIndex(pw => pw.ProviderId)
                .HasDatabaseName("IX_ProjectWorkspaces_ProviderId");

            builder.HasIndex(pw => pw.Status)
                .HasDatabaseName("IX_ProjectWorkspaces_Status");

            builder.HasIndex(pw => pw.CreatedAt)
                .HasDatabaseName("IX_ProjectWorkspaces_CreatedAt");

            // Table name
            builder.ToTable("ProjectWorkspaces");
        }
    }
}