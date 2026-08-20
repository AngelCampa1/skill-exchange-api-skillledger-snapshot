using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class ProcessedStripeWebhookEvent
{
    [MaxLength(255)]
    public string EventId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessingStartedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
