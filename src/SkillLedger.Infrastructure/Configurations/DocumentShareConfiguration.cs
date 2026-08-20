using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations
{
    public class DocumentShareConfiguration : IEntityTypeConfiguration<DocumentShare>
    {
        public void Configure(EntityTypeBuilder<DocumentShare> builder)
        {
            builder.ToTable("DocumentShares");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.Permission)
                .IsRequired()
                .HasDefaultValue(SharePermission.View)
                .HasConversion<int>()
                .HasSentinel(SharePermission.View);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.ShareMessage)
                .HasMaxLength(1000);

            builder.Property(e => e.AccessToken)
                .HasMaxLength(256);

            // Relationships
            builder.HasOne(e => e.Document)
                .WithMany(d => d.Shares)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Sharer)
                .WithMany()
                .HasForeignKey(e => e.SharedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Revoker)
                .WithMany()
                .HasForeignKey(e => e.RevokedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(e => e.DocumentId)
                .HasDatabaseName("IX_DocumentShares_DocumentId");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_DocumentShares_UserId");

            builder.HasIndex(e => e.SharedBy)
                .HasDatabaseName("IX_DocumentShares_SharedBy");

            builder.HasIndex(e => new { e.DocumentId, e.UserId })
                .HasDatabaseName("IX_DocumentShares_Document_User");

            builder.HasIndex(e => new { e.UserId, e.IsActive })
                .HasDatabaseName("IX_DocumentShares_User_Active");

            builder.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_DocumentShares_ExpiresAt");

            builder.HasIndex(e => e.AccessToken)
                .HasDatabaseName("IX_DocumentShares_AccessToken")
                .IsUnique()
                .HasFilter("[AccessToken] IS NOT NULL");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_DocumentShares_CreatedAt");
        }
    }
}