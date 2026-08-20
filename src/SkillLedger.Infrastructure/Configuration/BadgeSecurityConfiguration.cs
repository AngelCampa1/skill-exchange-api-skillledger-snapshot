namespace SkillLedger.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for badge security and integrity protection
/// </summary>
public class BadgeSecurityConfiguration
{
    public const string SectionName = "BadgeSecurity";

    /// <summary>
    /// Secret key for badge integrity hashing and encryption
    /// Should be retrieved from Azure Key Vault in production
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Badge verification code expiry time in hours
    /// Default: 24 hours
    /// </summary>
    public int VerificationCodeExpiryHours { get; set; } = 24;
}
