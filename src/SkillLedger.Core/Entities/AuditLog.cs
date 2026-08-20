using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class AuditLog
{
    public AuditLog()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user who performed the action (nullable for system actions)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Type of action performed (e.g., "USER_REGISTRATION", "USER_LOGIN")
    /// </summary>
    [MaxLength(100)]
    public required string Action { get; set; }

    /// <summary>
    /// Additional details about the action in JSON format
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    [MaxLength(45)]
    public string? IPAddress { get; set; }

    /// <summary>
    /// User agent string from the request
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Navigation property to the user (if applicable)
    /// </summary>
    public virtual User? User { get; set; }
}