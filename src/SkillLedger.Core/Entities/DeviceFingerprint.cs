using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Device fingerprint for fraud detection and security
/// </summary>
public class DeviceFingerprint
{
    public DeviceFingerprint()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who owns this device
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Cryptographic hash of device characteristics
    /// </summary>
    [MaxLength(256)]
    public string FingerprintHash { get; set; } = string.Empty;

    /// <summary>
    /// IP address when fingerprint was recorded
    /// </summary>
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent string
    /// </summary>
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Country code from IP geolocation
    /// </summary>
    [MaxLength(2)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Whether this device was used for registration
    /// </summary>
    public bool UsedForRegistration { get; set; }

    /// <summary>
    /// Whether this device was flagged as suspicious
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    /// Risk level assessed for this device
    /// </summary>
    public int RiskLevel { get; set; }

    /// <summary>
    /// Risk factors identified (JSON array)
    /// </summary>
    public string? RiskFactors { get; set; }

    /// <summary>
    /// When this fingerprint was first recorded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this fingerprint was last seen
    /// </summary>
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User? User { get; set; }
}

/// <summary>
/// Geographic location information for IP addresses
/// </summary>
public class IpGeolocation
{
    public IpGeolocation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// IP address (hashed for privacy)
    /// </summary>
    [MaxLength(256)]
    public string IpAddressHash { get; set; } = string.Empty;

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    [MaxLength(100)]
    public string CountryName { get; set; } = string.Empty;

    /// <summary>
    /// City name
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Region/state
    /// </summary>
    [MaxLength(100)]
    public string? Region { get; set; }

    /// <summary>
    /// Timezone identifier
    /// </summary>
    [MaxLength(50)]
    public string? Timezone { get; set; }

    /// <summary>
    /// Internet Service Provider
    /// </summary>
    [MaxLength(200)]
    public string? Isp { get; set; }

    /// <summary>
    /// Whether IP is from VPN service
    /// </summary>
    public bool IsVpn { get; set; }

    /// <summary>
    /// Whether IP is from proxy service
    /// </summary>
    public bool IsProxy { get; set; }

    /// <summary>
    /// Whether IP is from data center
    /// </summary>
    public bool IsDataCenter { get; set; }

    /// <summary>
    /// Whether this country is restricted for registration
    /// </summary>
    public bool IsRestricted { get; set; }

    /// <summary>
    /// When this geolocation data was recorded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this data expires (for caching)
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
}