using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for awarding a badge manually
/// </summary>
public class AwardBadgeRequestDto
{
    /// <summary>
    /// User ID to award badge to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of badge to award
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// Evidence for badge earning
    /// </summary>
    public Dictionary<string, object>? Evidence { get; set; }
}