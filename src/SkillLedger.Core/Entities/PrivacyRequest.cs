using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class PrivacyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(50)]
    public string RequestType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }

    [MaxLength(45)]
    public string? RequestedFromIp { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public string? EncryptedReason { get; set; }
    public string? EncryptedAdditionalNotes { get; set; }

    public bool ConfirmationRequired { get; set; }

    [MaxLength(128)]
    public string? ConfirmationTokenHash { get; set; }
}
