using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Entity representing a user's saved search with notification preferences
/// </summary>
public class SavedSearch
{
    public SavedSearch()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the saved search
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who saved this search
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User-defined name for this saved search
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description for this saved search
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Serialized search criteria (JSON)
    /// </summary>
    [Required]
    public string SearchCriteriaJson { get; set; } = null!;

    /// <summary>
    /// Serialized search criteria (JSON) - Alias for compatibility
    /// </summary>
    public string SearchCriteria
    {
        get => SearchCriteriaJson;
        set => SearchCriteriaJson = value;
    }

    /// <summary>
    /// Whether email notifications are enabled for this search
    /// </summary>
    public bool NotificationsEnabled { get; set; }

    /// <summary>
    /// Frequency of notifications for new matches
    /// </summary>
    public NotificationFrequency NotificationFrequency { get; set; } = NotificationFrequency.Daily;

    /// <summary>
    /// Last time notifications were sent for this search
    /// </summary>
    public DateTime? LastNotificationSentAt { get; set; }

    /// <summary>
    /// Number of times this search has been executed
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Number of times this search has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Last time this search was executed
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// Last time this search was used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether this saved search is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the saved search was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the saved search was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;
}