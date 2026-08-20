using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for submitting a verification request
/// </summary>
public class SubmitVerificationRequestDto
{
    /// <summary>
    /// Type of badge to verify
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BadgeType { get; set; } = string.Empty;

    /// <summary>
    /// Evidence submitted for verification
    /// </summary>
    public Dictionary<string, object>? Evidence { get; set; }
}