namespace SkillLedger.Core.DTOs;

/// <summary>
/// Result of external platform verification
/// </summary>
public class ExternalVerificationResult
{
    /// <summary>
    /// Platform name (LinkedIn, GitHub, etc.)
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Whether the verification was successful
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Verification confidence score (0.0 to 1.0)
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Professional data extracted from the platform
    /// </summary>
    public Dictionary<string, object> ProfessionalData { get; set; } = new();

    /// <summary>
    /// Error message if verification failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this verification was performed
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this verification expires and needs to be refreshed
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Raw response data from the external API (for audit purposes)
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Professional score calculated from platform data
    /// </summary>
    public int? ProfessionalScore { get; set; }

    /// <summary>
    /// Experience level determined from platform analysis
    /// </summary>
    public string? ExperienceLevel { get; set; }

    /// <summary>
    /// Skills extracted from the platform
    /// </summary>
    public List<string> ExtractedSkills { get; set; } = new();

    /// <summary>
    /// Industry/domain extracted from profile
    /// </summary>
    public string? Industry { get; set; }

    /// <summary>
    /// Years of experience calculated from platform data
    /// </summary>
    public int? YearsOfExperience { get; set; }
}