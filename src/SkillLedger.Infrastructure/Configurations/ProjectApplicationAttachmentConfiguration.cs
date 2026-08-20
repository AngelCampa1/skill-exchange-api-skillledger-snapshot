using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ProjectApplicationAttachmentConfiguration : IEntityTypeConfiguration<ProjectApplicationAttachment>
{
    public void Configure(EntityTypeBuilder<ProjectApplicationAttachment> builder)
    {
        builder.ToTable("ProjectApplicationAttachments");

        // Primary key
        builder.HasKey(paa => paa.Id);

        // Properties
        builder.Property(paa => paa.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(paa => paa.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(paa => paa.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(paa => paa.FileSize)
            .IsRequired();

        builder.Property(paa => paa.StorageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(paa => paa.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(paa => paa.IsVirusScanned)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(paa => paa.IsSafe)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(paa => paa.UploadedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(paa => paa.ProjectApplication)
            .WithMany(pa => pa.Attachments)
            .HasForeignKey(paa => paa.ProjectApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(paa => paa.ProjectApplicationId)
            .HasDatabaseName("IX_ProjectApplicationAttachments_ProjectApplicationId");

        builder.HasIndex(paa => paa.ContentType)
            .HasDatabaseName("IX_ProjectApplicationAttachments_ContentType");

        builder.HasIndex(paa => paa.IsVirusScanned)
            .HasDatabaseName("IX_ProjectApplicationAttachments_IsVirusScanned");

        builder.HasIndex(paa => paa.IsSafe)
            .HasDatabaseName("IX_ProjectApplicationAttachments_IsSafe");

        builder.HasIndex(paa => paa.UploadedAt)
            .HasDatabaseName("IX_ProjectApplicationAttachments_UploadedAt");
    }
}