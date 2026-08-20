using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for badge security and integrity protection
/// </summary>
public interface IBadgeSecurityService
{
    /// <summary>
    /// Generate a cryptographic hash for badge integrity
    /// </summary>
    /// <param name="badge">Badge to generate hash for</param>
    /// <returns>Integrity hash</returns>
    Task<string> GenerateBadgeHashAsync(UserBadge badge);

    /// <summary>
    /// Validate the integrity of a badge
    /// </summary>
    /// <param name="badge">Badge to validate</param>
    /// <returns>True if badge is valid</returns>
    Task<bool> ValidateBadgeIntegrityAsync(UserBadge badge);

    /// <summary>
    /// Generate a secure verification code for external verification
    /// </summary>
    /// <param name="badgeId">Badge ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Verification code</returns>
    Task<string> GenerateVerificationCodeAsync(Guid badgeId, Guid userId);

    /// <summary>
    /// Verify a badge using a verification code
    /// </summary>
    /// <param name="badgeId">Badge ID</param>
    /// <param name="verificationCode">Verification code</param>
    /// <returns>True if verification is valid</returns>
    Task<bool> VerifyBadgeCodeAsync(Guid badgeId, string verificationCode);

    /// <summary>
    /// Encrypt sensitive badge data
    /// </summary>
    /// <param name="data">Data to encrypt</param>
    /// <returns>Encrypted data</returns>
    Task<string> EncryptBadgeDataAsync(string data);

    /// <summary>
    /// Decrypt sensitive badge data
    /// </summary>
    /// <param name="encryptedData">Encrypted data</param>
    /// <returns>Decrypted data</returns>
    Task<string> DecryptBadgeDataAsync(string encryptedData);
}