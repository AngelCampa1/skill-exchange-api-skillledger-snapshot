using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations
{
    public class WorkspaceDocumentConfiguration : IEntityTypeConfiguration<WorkspaceDocument>
    {
        public void Configure(EntityTypeBuilder<WorkspaceDocument> builder)
        {
            builder.ToTable("WorkspaceDocuments");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(e => e.FileSize)
                .IsRequired();

            builder.Property(e => e.MimeType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.VersionNumber)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(e => e.SecurityScanPassed)
                .IsRequired()
                .HasDefaultValue(false);

            // Relationships
            builder.HasOne(e => e.Workspace)
                .WithMany()
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Uploader)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Folder)
                .WithMany(f => f.Documents)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Deleter)
                .WithMany()
                .HasForeignKey(e => e.DeletedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ParentDocument)
                .WithMany(e => e.PreviousVersions)
                .HasForeignKey(e => e.ParentDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Collection relationships
            builder.HasMany(e => e.AccessHistory)
                .WithOne(a => a.Document)
                .HasForeignKey(a => a.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Shares)
                .WithOne(s => s.Document)
                .HasForeignKey(s => s.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(e => e.WorkspaceId)
                .HasDatabaseName("IX_WorkspaceDocuments_WorkspaceId");

            builder.HasIndex(e => e.UploadedBy)
                .HasDatabaseName("IX_WorkspaceDocuments_UploadedBy");

            builder.HasIndex(e => e.FolderId)
                .HasDatabaseName("IX_WorkspaceDocuments_FolderId");

            builder.HasIndex(e => new { e.WorkspaceId, e.IsDeleted })
                .HasDatabaseName("IX_WorkspaceDocuments_Workspace_NotDeleted");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_WorkspaceDocuments_CreatedAt");

            builder.HasIndex(e => e.ParentDocumentId)
                .HasDatabaseName("IX_WorkspaceDocuments_ParentDocument");
        }
    }
}