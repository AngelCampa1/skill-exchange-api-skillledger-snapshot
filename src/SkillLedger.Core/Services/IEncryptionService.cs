using System.Security.Claims;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for handling field-level encryption of PII data
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypt sensitive PII data for storage
    /// </summary>
    /// <param name="plainText">Plain text to encrypt</param>
    /// <returns>Encrypted data</returns>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    /// Decrypt PII data for authorized access
    /// </summary>
    /// <param name="encryptedText">Encrypted data</param>
    /// <returns>Decrypted plain text</returns>
    Task<string> DecryptAsync(string encryptedText);

    /// <summary>
    /// Encrypt SSN/TIN with deterministic encryption for queries
    /// </summary>
    /// <param name="ssn">Social Security Number or Tax ID</param>
    /// <returns>Deterministically encrypted SSN</returns>
    Task<string> EncryptSsnAsync(string ssn);

    /// <summary>
    /// Hash PII for equality checks without storing plain text
    /// </summary>
    /// <param name="data">Data to hash</param>
    /// <returns>Cryptographic hash</returns>
    string HashPii(string data);

    /// <summary>
    /// Generate secure random tokens for verification
    /// </summary>
    /// <param name="length">Token length in bytes</param>
    /// <returns>Secure random token</returns>
    string GenerateSecureToken(int length = 32);
}

/// <summary>
/// Service for device fingerprinting and fraud detection
/// </summary>
public interface IDeviceFingerprintService
{
    /// <summary>
    /// Generate device fingerprint from browser/device characteristics
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="acceptLanguage">Accept-Language header</param>
    /// <param name="timezone">Client timezone</param>
    /// <param name="screenResolution">Screen resolution</param>
    /// <returns>Device fingerprint hash</returns>
    Task<string> GenerateFingerprintAsync(
        string userAgent,
        string ipAddress,
        string? acceptLanguage = null,
        string? timezone = null,
        string? screenResolution = null);

    /// <summary>
    /// Check if device fingerprint is suspicious (potential fraud)
    /// </summary>
    /// <param name="fingerprint">Device fingerprint</param>
    /// <param name="userId">User ID (if authenticated)</param>
    /// <returns>Risk assessment</returns>
    Task<DeviceRiskAssessment> AssessDeviceRiskAsync(string fingerprint, Guid? userId = null);

    /// <summary>
    /// Record device fingerprint for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="fingerprint">Device fingerprint</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="isRegistration">Whether this is during registration</param>
    /// <returns>Success indicator</returns>
    Task<bool> RecordDeviceAsync(Guid userId, string fingerprint, string ipAddress, bool isRegistration = false);
}

/// <summary>
/// Service for geographic restrictions and IP geolocation
/// </summary>
public interface IGeoLocationService
{
    /// <summary>
    /// Get country code from IP address
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>ISO 3166-1 alpha-2 country code</returns>
    Task<string?> GetCountryCodeAsync(string ipAddress);

    /// <summary>
    /// Check if country is restricted for registration
    /// </summary>
    /// <param name="countryCode">ISO country code</param>
    /// <returns>True if restricted</returns>
    bool IsRestrictedCountry(string countryCode);

    /// <summary>
    /// Check if IP is from a known VPN/proxy service
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>True if VPN/proxy detected</returns>
    Task<bool> IsVpnOrProxyAsync(string ipAddress);

    /// <summary>
    /// Get detailed location information
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Location details</returns>
    Task<LocationInfo> GetLocationInfoAsync(string ipAddress);
}

/// <summary>
/// Device risk assessment result
/// </summary>
public class DeviceRiskAssessment
{
    public RiskLevel RiskLevel { get; set; }
    public string[] RiskFactors { get; set; } = Array.Empty<string>();
    public bool RequiresAdditionalVerification { get; set; }
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Risk levels for device assessment
/// </summary>
public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Location information from IP geolocation
/// </summary>
public class LocationInfo
{
    public string? CountryCode { get; set; }
    public string? CountryName { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public string? Isp { get; set; }
    public bool IsVpn { get; set; }
    public bool IsProxy { get; set; }
    public bool IsDataCenter { get; set; }
}