using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for UploadedFile
/// </summary>
public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("UploadedFiles");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserId)
            .IsRequired();

        builder.Property(u => u.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FileSizeBytes)
            .IsRequired();

        builder.Property(u => u.BlobName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.ContainerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FileType)
            .IsRequired();

        builder.Property(u => u.IsApproved)
            .IsRequired();

        builder.Property(u => u.RequiresHumanReview)
            .IsRequired();

        builder.Property(u => u.SecurityScanPassed)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(u => u.UserId);
        builder.HasIndex(u => u.CreatedAt);
        builder.HasIndex(u => u.IsApproved);
        builder.HasIndex(u => u.FileType);
    }
}