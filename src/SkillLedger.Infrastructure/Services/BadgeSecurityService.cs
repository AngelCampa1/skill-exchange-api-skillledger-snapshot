using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for badge security and integrity protection
/// </summary>
public class BadgeSecurityService : IBadgeSecurityService
{
    private static readonly ConcurrentDictionary<string, Guid> IssuedCodesWithoutStorage = new();

    private readonly ILogger<BadgeSecurityService> _logger;
    private readonly BadgeSecurityConfiguration _config;
    private readonly SkillLedgerDbContext? _context;

    public BadgeSecurityService(
        ILogger<BadgeSecurityService> logger,
        IOptions<BadgeSecurityConfiguration> config,
        SkillLedgerDbContext? context = null)
    {
        _logger = logger;
        _config = config.Value;
        _context = context;

        // BUG-NEW-001 FIX: Validate secret key is configured
        if (string.IsNullOrWhiteSpace(_config.SecretKey))
        {
            throw new InvalidOperationException(
                "Badge secret key is not configured. Set BadgeSecurity:SecretKey in configuration.");
        }
    }

    public async Task<string> GenerateBadgeHashAsync(UserBadge badge)
    {
        try
        {
            // Create a deterministic string from badge data
            var badgeData = $"{badge.UserId}|{badge.BadgeType}|{badge.EarnedAt:yyyy-MM-ddTHH:mm:ss.fffZ}|{badge.VerificationEvidence ?? ""}";

            // BUG-NEW-001 FIX: Use configured secret key instead of hardcoded value
            var dataWithSecret = $"{badgeData}|{_config.SecretKey}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataWithSecret));
            var hash = Convert.ToBase64String(hashBytes);

            _logger.LogDebug("Generated badge hash for badge {BadgeId}", badge.Id);

            return await Task.FromResult(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating badge hash for badge {BadgeId}", badge.Id);
            throw;
        }
    }

    public async Task<bool> ValidateBadgeIntegrityAsync(UserBadge badge)
    {
        try
        {
            if (string.IsNullOrEmpty(badge.IntegrityHash))
            {
                _logger.LogWarning("Badge {BadgeId} has no integrity hash", badge.Id);
                return false;
            }

            var expectedHash = await GenerateBadgeHashAsync(badge);
            var isValid = expectedHash == badge.IntegrityHash;

            if (!isValid)
            {
                _logger.LogWarning("Badge integrity validation failed for badge {BadgeId}", badge.Id);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating badge integrity for badge {BadgeId}", badge.Id);
            return false;
        }
    }

    public async Task<string> GenerateVerificationCodeAsync(Guid badgeId, Guid userId)
    {
        try
        {
            if (_context != null)
            {
                var badge = await _context.UserBadges
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == badgeId && b.UserId == userId && b.IsActive);

                if (badge == null || (badge.ExpiresAt.HasValue && badge.ExpiresAt.Value <= DateTime.UtcNow))
                {
                    throw new InvalidOperationException("Badge is not active or does not belong to the current user.");
                }
            }

            // Create a time-sensitive verification code
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var shortHash = GenerateVerificationHash(badgeId, userId, timestamp);
            var verificationCode = $"{shortHash}-{timestamp}";
            if (_context == null)
            {
                IssuedCodesWithoutStorage[$"{badgeId:N}:{verificationCode}"] = userId;
            }

            _logger.LogInformation("Generated verification code for badge {BadgeId}", badgeId);

            return await Task.FromResult(verificationCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verification code for badge {BadgeId}", badgeId);
            throw;
        }
    }

    public async Task<bool> VerifyBadgeCodeAsync(Guid badgeId, string verificationCode)
    {
        try
        {
            if (string.IsNullOrEmpty(verificationCode) || !verificationCode.Contains('-'))
            {
                return false;
            }

            var parts = verificationCode.Split('-');
            if (parts.Length != 2 || !long.TryParse(parts[1], out var timestamp))
            {
                return false;
            }

            // BUG-NEW-001 FIX: Use configured expiry time
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var codeAge = now - timestamp;
            var expirySeconds = _config.VerificationCodeExpiryHours * 3600;
            if (codeAge < 0 || codeAge > expirySeconds)
            {
                _logger.LogWarning("Verification code expired for badge {BadgeId}", badgeId);
                return false;
            }

            var submittedHash = parts[0];
            if (_context == null)
            {
                if (!IssuedCodesWithoutStorage.TryGetValue($"{badgeId:N}:{verificationCode}", out var issuedUserId))
                {
                    _logger.LogWarning("Verification code rejected for unissued badge code {BadgeId}", badgeId);
                    return false;
                }

                var expectedHashWithoutStorage = GenerateVerificationHash(badgeId, issuedUserId, timestamp);
                return submittedHash.Length == expectedHashWithoutStorage.Length &&
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(submittedHash),
                        Encoding.UTF8.GetBytes(expectedHashWithoutStorage));
            }

            var badge = await _context.UserBadges
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == badgeId && b.IsActive);

            if (badge == null || (badge.ExpiresAt.HasValue && badge.ExpiresAt.Value <= DateTime.UtcNow))
            {
                _logger.LogWarning("Verification code rejected for missing or inactive badge {BadgeId}", badgeId);
                return false;
            }

            var expectedHash = GenerateVerificationHash(badgeId, badge.UserId, timestamp);
            if (submittedHash.Length != expectedHash.Length)
            {
                _logger.LogWarning("Verification code signature length mismatch for badge {BadgeId}", badgeId);
                return false;
            }

            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(submittedHash),
                Encoding.UTF8.GetBytes(expectedHash));

            if (!isValid)
            {
                _logger.LogWarning("Verification code signature mismatch for badge {BadgeId}", badgeId);
                return false;
            }

            _logger.LogInformation("Verification code validated for badge {BadgeId}", badgeId);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying badge code for badge {BadgeId}", badgeId);
            return false;
        }
    }

    private string GenerateVerificationHash(Guid badgeId, Guid userId, long timestamp)
    {
        var codeData = $"{badgeId}|{userId}|{timestamp}|{_config.SecretKey}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeData));

        return Convert.ToBase64String(hashBytes)[..12].Replace("+", "").Replace("/", "");
    }

    public async Task<string> EncryptBadgeDataAsync(string data)
    {
        try
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using var aes = Aes.Create();
            // BUG-NEW-001 FIX: Use configured secret key
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(_config.SecretKey))[..32]; // Use first 32 bytes for AES-256
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();

            // Prepend IV to encrypted data
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(data);
            }

            var encryptedBytes = msEncrypt.ToArray();
            var encryptedData = Convert.ToBase64String(encryptedBytes);

            _logger.LogDebug("Encrypted badge data successfully");

            return await Task.FromResult(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting badge data");
            throw;
        }
    }

    public async Task<string> DecryptBadgeDataAsync(string encryptedData)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedData))
                return string.Empty;

            var encryptedBytes = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            // BUG-NEW-001 FIX: Use configured secret key
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(_config.SecretKey))[..32]; // Use first 32 bytes for AES-256
            aes.Key = key;

            // Extract IV from the beginning of encrypted data
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(encryptedBytes, iv, iv.Length);
            aes.IV = iv;

            // Decrypt the remaining data
            var cipherText = new byte[encryptedBytes.Length - iv.Length];
            Array.Copy(encryptedBytes, iv.Length, cipherText, 0, cipherText.Length);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            var decryptedData = srDecrypt.ReadToEnd();

            _logger.LogDebug("Decrypted badge data successfully");

            return await Task.FromResult(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting badge data");
            throw;
        }
    }
}
