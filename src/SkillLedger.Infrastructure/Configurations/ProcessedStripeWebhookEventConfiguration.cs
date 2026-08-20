using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ProcessedStripeWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedStripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedStripeWebhookEvent> builder)
    {
        builder.ToTable("ProcessedStripeWebhookEvents");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasIndex(e => e.ProcessedAt)
            .HasDatabaseName("IX_ProcessedStripeWebhookEvents_ProcessedAt");
    }
}
