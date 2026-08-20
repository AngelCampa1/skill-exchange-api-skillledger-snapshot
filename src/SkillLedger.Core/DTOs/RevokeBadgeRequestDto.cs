using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for revoking a badge
/// </summary>
public class RevokeBadgeRequestDto
{
    /// <summary>
    /// Reason for badge revocation
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}