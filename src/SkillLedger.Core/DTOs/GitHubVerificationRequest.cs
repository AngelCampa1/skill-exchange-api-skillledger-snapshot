using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// Request for GitHub contributions verification
/// </summary>
public class GitHubVerificationRequest
{
    /// <summary>
    /// GitHub username to verify
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9]([a-zA-Z0-9]|-(?=[a-zA-Z0-9])){0,38}$",
        ErrorMessage = "Invalid GitHub username format")]
    public string GitHubUsername { get; set; } = string.Empty;

    /// <summary>
    /// Badge type being requested through this verification
    /// </summary>
    public string? BadgeType { get; set; }

    /// <summary>
    /// Specific repositories to analyze (optional)
    /// </summary>
    public List<string>? RepositoriesToAnalyze { get; set; }

    /// <summary>
    /// Additional evidence or notes
    /// </summary>
    public Dictionary<string, object>? AdditionalEvidence { get; set; }
}