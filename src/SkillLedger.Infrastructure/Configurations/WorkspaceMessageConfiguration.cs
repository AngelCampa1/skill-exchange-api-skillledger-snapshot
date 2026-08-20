using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for WorkspaceMessage entity
    /// </summary>
    public class WorkspaceMessageConfiguration : IEntityTypeConfiguration<WorkspaceMessage>
    {
        public void Configure(EntityTypeBuilder<WorkspaceMessage> builder)
        {
            // Primary key
            builder.HasKey(m => m.Id);

            // Table configuration
            builder.ToTable("WorkspaceMessages");

            // Required properties
            builder.Property(m => m.WorkspaceId)
                .IsRequired();

            builder.Property(m => m.SenderId)
                .IsRequired();

            builder.Property(m => m.MessageType)
                .IsRequired()
                .HasConversion<int>(); // Store enum as int

            builder.Property(m => m.Status)
                .IsRequired()
                .HasConversion<int>(); // Store enum as int

            builder.Property(m => m.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Optional properties with constraints
            builder.Property(m => m.MessageText)
                .HasMaxLength(4000)
                .IsRequired(false);

            builder.Property(m => m.AttachmentUrl)
                .HasMaxLength(500)
.IsRequired(false);

            builder.Property(m => m.AttachmentFileName)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(m => m.AttachmentMimeType)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(m => m.SenderIpAddress)
                .HasMaxLength(45) // Support IPv6
                .IsRequired(false);

            builder.Property(m => m.SenderUserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            // Boolean properties
            builder.Property(m => m.IsEdited)
                .IsRequired()
                .HasDefaultValue(false);

            // DateTime properties
            builder.Property(m => m.EditedAt)
                .IsRequired(false);

            builder.Property(m => m.ReadAt)
                .IsRequired(false);

            // Numeric properties
            builder.Property(m => m.AttachmentSize)
                .IsRequired(false);

            // Foreign key relationships
            builder.HasOne(m => m.Workspace)
                .WithMany()
                .HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Self-referencing relationship for replies
            builder.HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Navigation properties
            builder.HasMany(m => m.Reactions)
                .WithOne(r => r.Message)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            builder.HasIndex(m => m.WorkspaceId)
                .HasDatabaseName("IX_WorkspaceMessages_WorkspaceId");

            builder.HasIndex(m => m.SenderId)
                .HasDatabaseName("IX_WorkspaceMessages_SenderId");

            builder.HasIndex(m => m.CreatedAt)
                .HasDatabaseName("IX_WorkspaceMessages_CreatedAt");

            builder.HasIndex(m => new { m.WorkspaceId, m.CreatedAt })
                .HasDatabaseName("IX_WorkspaceMessages_WorkspaceId_CreatedAt");

            // Index for message search
            builder.HasIndex(m => m.MessageText)
                .HasDatabaseName("IX_WorkspaceMessages_MessageText")
                .HasFilter("MessageText IS NOT NULL");

            // Composite index for read status tracking
            builder.HasIndex(m => new { m.WorkspaceId, m.Status, m.SenderId })
                .HasDatabaseName("IX_WorkspaceMessages_Status_Tracking");

            // Check constraints
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_WorkspaceMessages_MessageType",
                "[MessageType] IN (0, 1, 2, 3, 4, 5)"));

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_WorkspaceMessages_Status",
                "[Status] IN (0, 1, 2, 3, 4)"));

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_WorkspaceMessages_AttachmentSize",
                "[AttachmentSize] IS NULL OR [AttachmentSize] > 0"));

            // Ensure text messages have text content
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_WorkspaceMessages_TextContent",
                "[MessageType] != 0 OR [MessageText] IS NOT NULL"));

            // Ensure file messages have attachment URL
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_WorkspaceMessages_FileContent",
                "[MessageType] NOT IN (1, 4, 5) OR [AttachmentUrl] IS NOT NULL"));
        }
    }
}