using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// Request for LinkedIn profile verification
/// </summary>
public class LinkedInVerificationRequest
{
    /// <summary>
    /// LinkedIn profile URL to verify
    /// </summary>
    [Required]
    [Url]
    public string LinkedInUrl { get; set; } = string.Empty;

    /// <summary>
    /// Badge type being requested through this verification
    /// </summary>
    public string? BadgeType { get; set; }

    /// <summary>
    /// Additional evidence or notes
    /// </summary>
    public Dictionary<string, object>? AdditionalEvidence { get; set; }
}