using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for integrating with external professional networks and platforms
/// </summary>
public interface IExternalIntegrationService
{
    /// <summary>
    /// Verify LinkedIn profile authenticity and extract professional information
    /// </summary>
    /// <param name="linkedInUrl">LinkedIn profile URL</param>
    /// <returns>Verification result with professional data</returns>
    Task<ExternalVerificationResult> VerifyLinkedInProfileAsync(string linkedInUrl);

    /// <summary>
    /// Verify GitHub contributions and analyze repository activity
    /// </summary>
    /// <param name="githubUsername">GitHub username</param>
    /// <returns>Verification result with contribution metrics</returns>
    Task<ExternalVerificationResult> VerifyGitHubContributionsAsync(string githubUsername);

    /// <summary>
    /// Check if external integration is enabled for the specified platform
    /// </summary>
    /// <param name="platform">Platform name (LinkedIn, GitHub, etc.)</param>
    /// <returns>True if integration is enabled</returns>
    bool IsIntegrationEnabled(string platform);

    /// <summary>
    /// Get cached verification result if available and not expired
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="platform">Platform name</param>
    /// <returns>Cached result or null if not available</returns>
    Task<ExternalVerificationResult?> GetCachedVerificationAsync(Guid userId, string platform);

    /// <summary>
    /// Store verification result in cache
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="platform">Platform name</param>
    /// <param name="result">Verification result to cache</param>
    Task CacheVerificationResultAsync(Guid userId, string platform, ExternalVerificationResult result);
}