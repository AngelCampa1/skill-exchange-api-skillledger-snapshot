using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Implementation of field-level encryption service using Azure Key Vault
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly IAzureKeyVaultService _keyVaultService;
    private readonly EncryptionConfiguration _config;

    public EncryptionService(
        ILogger<EncryptionService> logger,
        IAzureKeyVaultService keyVaultService,
        IOptions<EncryptionConfiguration> config)
    {
        _logger = logger;
        _keyVaultService = keyVaultService;
        _config = config.Value;
    }

    /// <summary>
    /// Encrypt sensitive PII data using AES-256-GCM
    /// </summary>
    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            // Get data encryption key from Key Vault
            var keyBytes = await _keyVaultService.GetDataEncryptionKeyAsync();

            var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
            RandomNumberGenerator.Fill(nonce);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

            using var aes = new AesGcm(keyBytes, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine nonce + tag + ciphertext
            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return await Task.FromResult(Convert.ToBase64String(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt PII data");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypt PII data using AES-256-GCM (with backward-compatible fallback for legacy AES-CBC data)
    /// </summary>
    public async Task<string> DecryptAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var data = Convert.FromBase64String(encryptedText);
            var keyBytes = await _keyVaultService.GetDataEncryptionKeyAsync();

            // GCM format: nonce(12) + tag(16) + ciphertext(N) = minimum 28 bytes
            // CBC format: IV(16) + ciphertext(multiple of 16) = minimum 32 bytes, ciphertext always % 16 == 0
            // Detect format: if total length minus 28 (GCM overhead) is NOT a multiple of 16,
            // or data length >= 32 and (data.Length - 16) % 16 == 0 and GCM decryption fails,
            // fall back to CBC.
            try
            {
                return DecryptGcm(data, keyBytes);
            }
            catch (CryptographicException)
            {
                // GCM decryption failed — try legacy CBC fallback for pre-migration data
                _logger.LogWarning("GCM decryption failed, attempting legacy CBC fallback for backward compatibility");
                return DecryptCbcLegacy(data, keyBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt PII data");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    private static string DecryptGcm(byte[] data, byte[] keyBytes)
    {
        var nonce = data[..AesGcm.NonceByteSizes.MaxSize];
        var tag = data[AesGcm.NonceByteSizes.MaxSize..(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)];
        var ciphertext = data[(AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize)..];

        using var aes = new AesGcm(keyBytes, AesGcm.TagByteSizes.MaxSize);
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static string DecryptCbcLegacy(byte[] data, byte[] keyBytes)
    {
        // Legacy CBC format: IV (first 16 bytes) + ciphertext (rest)
        using var aesAlg = Aes.Create();
        aesAlg.Key = keyBytes;
        aesAlg.IV = data[..16];
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        var plaintext = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Encrypt SSN/TIN with deterministic encryption for database queries
    /// </summary>
    public async Task<string> EncryptSsnAsync(string ssn)
    {
        if (string.IsNullOrEmpty(ssn))
            return string.Empty;

        try
        {
            // Use HMAC-SHA256 with Key Vault key for deterministic encryption
            var key = await _keyVaultService.GetSsnEncryptionKeyAsync();

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ssn));

            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt SSN/TIN");
            throw new InvalidOperationException("SSN encryption failed", ex);
        }
    }

    /// <summary>
    /// Hash PII for equality checks using PBKDF2
    /// </summary>
    public string HashPii(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        try
        {
            // Use PBKDF2 with random salt
            var salt = RandomNumberGenerator.GetBytes(32);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(data),
                salt,
                100000, // iterations
                HashAlgorithmName.SHA256,
                32); // output length

            // Combine salt + hash
            var result = new byte[salt.Length + hash.Length];
            salt.CopyTo(result, 0);
            hash.CopyTo(result, salt.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash PII data");
            throw new InvalidOperationException("PII hashing failed", ex);
        }
    }

    /// <summary>
    /// Generate cryptographically secure random token
    /// </summary>
    public string GenerateSecureToken(int length = 32)
    {
        try
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('='); // URL-safe base64
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate secure token");
            throw new InvalidOperationException("Token generation failed", ex);
        }
    }
}

/// <summary>
/// Device fingerprinting service for fraud detection
/// </summary>
public class DeviceFingerprintService : IDeviceFingerprintService
{
    private readonly ILogger<DeviceFingerprintService> _logger;
    private readonly SkillLedgerDbContext _context;
    private readonly IGeoLocationService _geoLocationService;

    public DeviceFingerprintService(
        ILogger<DeviceFingerprintService> logger,
        SkillLedgerDbContext context,
        IGeoLocationService geoLocationService)
    {
        _logger = logger;
        _context = context;
        _geoLocationService = geoLocationService;
    }

    /// <summary>
    /// Generate device fingerprint from browser characteristics
    /// </summary>
    public Task<string> GenerateFingerprintAsync(
        string userAgent,
        string ipAddress,
        string? acceptLanguage = null,
        string? timezone = null,
        string? screenResolution = null)
    {
        try
        {
            var fingerprintData = new
            {
                UserAgent = NormalizeUserAgent(userAgent),
                IpSubnet = GetIpSubnet(ipAddress),
                AcceptLanguage = acceptLanguage?.ToLowerInvariant(),
                Timezone = timezone,
                ScreenResolution = screenResolution
            };

            var json = JsonSerializer.Serialize(fingerprintData);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));

            return Task.FromResult(Convert.ToHexString(hash).ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate device fingerprint");
            throw new InvalidOperationException("Fingerprint generation failed", ex);
        }
    }

    /// <summary>
    /// Assess device risk based on fingerprint and historical data
    /// </summary>
    public async Task<DeviceRiskAssessment> AssessDeviceRiskAsync(string fingerprint, Guid? userId = null)
    {
        try
        {
            var assessment = new DeviceRiskAssessment
            {
                RiskLevel = RiskLevel.Low,
                RiskFactors = Array.Empty<string>()
            };

            var riskFactors = new List<string>();

            // Check if this device has been seen before
            var existingDevice = await _context.DeviceFingerprints
                .FirstOrDefaultAsync(d => d.FingerprintHash == fingerprint);

            if (existingDevice == null)
            {
                assessment.RiskLevel = RiskLevel.Medium;
                riskFactors.Add("New device");
            }
            else
            {
                // Check if device was previously flagged
                if (existingDevice.IsSuspicious)
                {
                    assessment.RiskLevel = RiskLevel.High;
                    riskFactors.Add("Previously flagged device");
                }

                // Check if device is associated with different users
                var deviceUserCount = await _context.DeviceFingerprints
                    .Where(d => d.FingerprintHash == fingerprint && d.UserId.HasValue)
                    .Select(d => d.UserId)
                    .Distinct()
                    .CountAsync();

                if (deviceUserCount > 3)
                {
                    assessment.RiskLevel = RiskLevel.High;
                    riskFactors.Add("Shared device with many users");
                }
            }

            // Check for suspicious registration patterns
            if (userId.HasValue)
            {
                var recentRegistrations = await _context.DeviceFingerprints
                    .Where(d => d.UsedForRegistration &&
                               d.CreatedAt > DateTime.UtcNow.AddHours(-24))
                    .CountAsync();

                if (recentRegistrations > 10)
                {
                    assessment.RiskLevel = RiskLevel.Critical;
                    riskFactors.Add("High registration volume");
                }
            }

            assessment.RiskFactors = riskFactors.ToArray();
            assessment.RequiresAdditionalVerification = assessment.RiskLevel >= RiskLevel.High;

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess device risk");
            return new DeviceRiskAssessment
            {
                RiskLevel = RiskLevel.Critical,
                RiskFactors = new[] { "Risk assessment failed" },
                RequiresAdditionalVerification = true
            };
        }
    }

    /// <summary>
    /// Record device fingerprint for user
    /// </summary>
    public async Task<bool> RecordDeviceAsync(Guid userId, string fingerprint, string ipAddress, bool isRegistration = false)
    {
        try
        {
            var countryCode = await _geoLocationService.GetCountryCodeAsync(ipAddress);
            var riskAssessment = await AssessDeviceRiskAsync(fingerprint, userId);

            var deviceFingerprint = new DeviceFingerprint
            {
                UserId = userId,
                FingerprintHash = fingerprint,
                IpAddress = ipAddress,
                CountryCode = countryCode,
                UsedForRegistration = isRegistration,
                IsSuspicious = riskAssessment.RiskLevel >= RiskLevel.High,
                RiskLevel = (int)riskAssessment.RiskLevel,
                RiskFactors = JsonSerializer.Serialize(riskAssessment.RiskFactors)
            };

            _context.DeviceFingerprints.Add(deviceFingerprint);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record device fingerprint for user {UserId}", userId);
            return false;
        }
    }

    private static string NormalizeUserAgent(string userAgent)
    {
        // Remove version numbers to reduce fingerprint uniqueness
        var normalized = Regex.Replace(userAgent, @"\d+\.\d+\.\d+", "X.X.X");
        normalized = Regex.Replace(normalized, @"\d+\.\d+", "X.X");

        // Limit length
        return normalized.Length > 200 ? normalized[..200] : normalized;
    }

    private static string GetIpSubnet(string ipAddress)
    {
        // Use /24 subnet for IPv4, /64 for IPv6 to reduce fingerprint uniqueness
        if (IPAddress.TryParse(ipAddress, out var ip))
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var bytes = ip.GetAddressBytes();
                var subnet = new byte[8];
                Array.Copy(bytes, 0, subnet, 0, 8);
                return $"{new IPAddress(subnet)}/64";
            }
        }
        return ipAddress;
    }
}