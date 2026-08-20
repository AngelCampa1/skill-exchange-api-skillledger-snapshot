using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations
{
    public class DocumentAccessConfiguration : IEntityTypeConfiguration<DocumentAccess>
    {
        public void Configure(EntityTypeBuilder<DocumentAccess> builder)
        {
            builder.ToTable("DocumentAccesses");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.AccessType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("view");

            builder.Property(e => e.AccessedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            builder.Property(e => e.UserAgent)
                .HasMaxLength(500);

            // Relationships
            builder.HasOne(e => e.Document)
                .WithMany(d => d.AccessHistory)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(e => e.DocumentId)
                .HasDatabaseName("IX_DocumentAccesses_DocumentId");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_DocumentAccesses_UserId");

            builder.HasIndex(e => e.AccessedAt)
                .HasDatabaseName("IX_DocumentAccesses_AccessedAt");

            builder.HasIndex(e => new { e.DocumentId, e.UserId })
                .HasDatabaseName("IX_DocumentAccesses_Document_User");

            builder.HasIndex(e => new { e.UserId, e.AccessedAt })
                .HasDatabaseName("IX_DocumentAccesses_User_AccessedAt");
        }
    }
}