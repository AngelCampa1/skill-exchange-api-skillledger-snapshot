using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for TypingIndicator entity
    /// </summary>
    public class TypingIndicatorConfiguration : IEntityTypeConfiguration<TypingIndicator>
    {
        public void Configure(EntityTypeBuilder<TypingIndicator> builder)
        {
            // Primary key
            builder.HasKey(t => t.Id);

            // Table configuration
            builder.ToTable("TypingIndicators");

            // Required properties
            builder.Property(t => t.WorkspaceId)
                .IsRequired();

            builder.Property(t => t.UserId)
                .IsRequired();

            builder.Property(t => t.LastTypingAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // Optional properties
            builder.Property(t => t.ConnectionId)
                .HasMaxLength(100)
                .IsRequired(false);

            // Foreign key relationships
            builder.HasOne(t => t.Workspace)
                .WithMany()
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            builder.HasIndex(t => t.WorkspaceId)
                .HasDatabaseName("IX_TypingIndicators_WorkspaceId");

            builder.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_TypingIndicators_UserId");

            builder.HasIndex(t => t.LastTypingAt)
                .HasDatabaseName("IX_TypingIndicators_LastTypingAt");

            // Index for cleanup queries (finding inactive typing indicators)
            builder.HasIndex(t => new { t.WorkspaceId, t.LastTypingAt })
                .HasDatabaseName("IX_TypingIndicators_WorkspaceId_LastTypingAt");

            // Unique constraint: One typing indicator per user per workspace
            builder.HasIndex(t => new { t.WorkspaceId, t.UserId })
                .IsUnique()
                .HasDatabaseName("IX_TypingIndicators_WorkspaceId_UserId_Unique");

            // Index for SignalR connection tracking
            builder.HasIndex(t => t.ConnectionId)
                .HasDatabaseName("IX_TypingIndicators_ConnectionId")
                .HasFilter("ConnectionId IS NOT NULL");
        }
    }
}