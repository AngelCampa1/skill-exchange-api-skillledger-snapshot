using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for MessageReaction entity
    /// </summary>
    public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
    {
        public void Configure(EntityTypeBuilder<MessageReaction> builder)
        {
            // Primary key
            builder.HasKey(r => r.Id);

            // Table configuration
            builder.ToTable("MessageReactions");

            // Required properties
            builder.Property(r => r.MessageId)
                .IsRequired();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.Emoji)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Optional properties
            builder.Property(r => r.IpAddress)
                .HasMaxLength(45) // Support IPv6
                .IsRequired(false);

            // Foreign key relationships
            builder.HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Indexes for performance
            builder.HasIndex(r => r.MessageId)
                .HasDatabaseName("IX_MessageReactions_MessageId");

            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_MessageReactions_UserId");

            builder.HasIndex(r => r.CreatedAt)
                .HasDatabaseName("IX_MessageReactions_CreatedAt");

            // Composite index for reaction grouping
            builder.HasIndex(r => new { r.MessageId, r.Emoji })
                .HasDatabaseName("IX_MessageReactions_MessageId_Emoji");

            // Unique constraint: One reaction per user per message per emoji
            builder.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji })
                .IsUnique()
                .HasDatabaseName("IX_MessageReactions_MessageId_UserId_Emoji_Unique");

            // Check constraints
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_MessageReactions_EmojiLength",
                "LEN([Emoji]) > 0"));
        }
    }
}