using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations
{
    public class DocumentFolderConfiguration : IEntityTypeConfiguration<DocumentFolder>
    {
        public void Configure(EntityTypeBuilder<DocumentFolder> builder)
        {
            builder.ToTable("DocumentFolders");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.FolderName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .HasMaxLength(1000);

            builder.Property(e => e.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Relationships
            builder.HasOne(e => e.Workspace)
                .WithMany()
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ParentFolder)
                .WithMany(f => f.ChildFolders)
                .HasForeignKey(e => e.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Deleter)
                .WithMany()
                .HasForeignKey(e => e.DeletedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Collection relationships are already defined in the entities

            // Indexes
            builder.HasIndex(e => e.WorkspaceId)
                .HasDatabaseName("IX_DocumentFolders_WorkspaceId");

            builder.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_DocumentFolders_CreatedBy");

            builder.HasIndex(e => e.ParentFolderId)
                .HasDatabaseName("IX_DocumentFolders_ParentFolderId");

            builder.HasIndex(e => new { e.WorkspaceId, e.IsDeleted })
                .HasDatabaseName("IX_DocumentFolders_Workspace_NotDeleted");

            builder.HasIndex(e => new { e.WorkspaceId, e.ParentFolderId, e.FolderName })
                .HasDatabaseName("IX_DocumentFolders_UniqueName")
                .IsUnique()
                .HasFilter("[IsDeleted] = 0"); // Only enforce uniqueness for non-deleted folders

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_DocumentFolders_CreatedAt");
        }
    }
}