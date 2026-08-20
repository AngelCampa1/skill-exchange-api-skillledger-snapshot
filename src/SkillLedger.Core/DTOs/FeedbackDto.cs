using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// Categories for user feedback submissions
/// </summary>
public enum FeedbackCategory
{
    General = 0,
    Bug = 1,
    FeatureRequest = 2,
    Other = 3
}

/// <summary>
/// DTO for submitting user feedback
/// </summary>
public class SubmitFeedbackDto
{
    /// <summary>
    /// The category of feedback being submitted
    /// </summary>
    [Required(ErrorMessage = "Category is required")]
    public FeedbackCategory Category { get; set; }

    /// <summary>
    /// The feedback message content
    /// </summary>
    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional email address for reply (for anonymous users or if different from account email)
    /// </summary>
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters")]
    public string? ReplyToEmail { get; set; }
}

/// <summary>
/// Response DTO for feedback submission
/// </summary>
public class FeedbackResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
