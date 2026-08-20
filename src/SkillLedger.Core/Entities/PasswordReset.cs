using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Entity representing a password reset request
/// </summary>
public class PasswordReset
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user requesting password reset
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Secure token for password reset verification
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Hashed version of the token for secure storage
    /// </summary>
    [Required]
    [StringLength(512)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// When the reset request was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the reset token expires (default 1 hour)
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the reset token has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// When the token was used (if applicable)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address from which the reset was requested
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent string from the reset request
    /// </summary>
    [StringLength(1000)]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Number of attempts made with this token
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// When the last attempt was made
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Check if the reset token is still valid
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Check if the reset token has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}