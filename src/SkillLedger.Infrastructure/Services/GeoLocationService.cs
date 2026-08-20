using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for IP geolocation and geographic restrictions
/// </summary>
public class GeoLocationService : IGeoLocationService
{
    private readonly ILogger<GeoLocationService> _logger;
    private readonly SkillLedgerDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly GeoLocationConfiguration _config;

    // Countries restricted for registration (high-risk jurisdictions)
    private static readonly HashSet<string> RestrictedCountries = new()
    {
        "AF", // Afghanistan
        "BY", // Belarus
        "MM", // Myanmar
        "KP", // North Korea
        "RU", // Russia
        "IR", // Iran
        "SY", // Syria
        "CU", // Cuba
        "SD", // Sudan
        "VE", // Venezuela
        "ZW", // Zimbabwe
        "CF", // Central African Republic
        "CD", // Democratic Republic of Congo
        "GQ", // Equatorial Guinea
        "ER", // Eritrea
        "GN", // Guinea
        "GW", // Guinea-Bissau
        "HT", // Haiti
        "IQ", // Iraq
        "LB", // Lebanon
        "LR", // Liberia
        "LY", // Libya
        "ML", // Mali
        "NI", // Nicaragua
        "SO", // Somalia
        "SS", // South Sudan
        "YE"  // Yemen
    };

    public GeoLocationService(
        ILogger<GeoLocationService> logger,
        SkillLedgerDbContext context,
        HttpClient httpClient,
        IOptions<GeoLocationConfiguration> config)
    {
        _logger = logger;
        _context = context;
        _httpClient = httpClient;
        _config = config.Value;
    }

    /// <summary>
    /// Get country code from IP address using multiple providers
    /// </summary>
    public async Task<string?> GetCountryCodeAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || IsLocalIpAddress(ipAddress))
            return "US"; // Default for local IPs

        try
        {
            // Check cache first
            var cachedLocation = await GetCachedLocationAsync(ipAddress);
            if (cachedLocation != null && cachedLocation.ExpiresAt > DateTime.UtcNow)
            {
                return cachedLocation.CountryCode;
            }

            // Try primary geolocation service
            var locationInfo = await GetLocationFromPrimaryServiceAsync(ipAddress);
            if (locationInfo != null)
            {
                await CacheLocationAsync(ipAddress, locationInfo);
                return locationInfo.CountryCode;
            }

            // Fallback to secondary service
            locationInfo = await GetLocationFromFallbackServiceAsync(ipAddress);
            if (locationInfo != null)
            {
                await CacheLocationAsync(ipAddress, locationInfo);
                return locationInfo.CountryCode;
            }

            _logger.LogWarning("Could not determine country for IP {IpAddress}", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting country code for IP {IpAddress}", ipAddress);
            return null;
        }
    }

    /// <summary>
    /// Check if country is restricted for registration
    /// </summary>
    public bool IsRestrictedCountry(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return true; // Restrict if we can't determine country

        return RestrictedCountries.Contains(countryCode.ToUpperInvariant());
    }

    /// <summary>
    /// Check if IP is from VPN/proxy using threat intelligence
    /// </summary>
    public async Task<bool> IsVpnOrProxyAsync(string ipAddress)
    {
        try
        {
            var locationInfo = await GetLocationInfoAsync(ipAddress);
            return locationInfo?.IsVpn == true || locationInfo?.IsProxy == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking VPN/proxy status for IP {IpAddress}", ipAddress);
            return false; // Default to not VPN if we can't determine
        }
    }

    /// <summary>
    /// Get comprehensive location information
    /// </summary>
    public async Task<LocationInfo> GetLocationInfoAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || IsLocalIpAddress(ipAddress))
        {
            return new LocationInfo
            {
                CountryCode = "US",
                CountryName = "United States",
                IsVpn = false,
                IsProxy = false
            };
        }

        try
        {
            // Check cache first
            var cached = await GetCachedLocationAsync(ipAddress);
            if (cached != null && cached.ExpiresAt > DateTime.UtcNow)
            {
                return new LocationInfo
                {
                    CountryCode = cached.CountryCode,
                    CountryName = cached.CountryName,
                    City = cached.City,
                    Region = cached.Region,
                    Timezone = cached.Timezone,
                    Isp = cached.Isp,
                    IsVpn = cached.IsVpn,
                    IsProxy = cached.IsProxy,
                    IsDataCenter = cached.IsDataCenter
                };
            }

            // Get fresh data
            var locationInfo = await GetLocationFromPrimaryServiceAsync(ipAddress)
                            ?? await GetLocationFromFallbackServiceAsync(ipAddress);

            if (locationInfo != null)
            {
                await CacheLocationAsync(ipAddress, locationInfo);
            }

            return locationInfo ?? new LocationInfo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location info for IP {IpAddress}", ipAddress);
            return new LocationInfo();
        }
    }

    private async Task<LocationInfo?> GetLocationFromPrimaryServiceAsync(string ipAddress)
    {
        try
        {
            // Using ip-api.com (free tier with reasonable limits)
            var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}?fields=status,message,country,countryCode,region,city,timezone,isp,proxy,hosting");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.GetProperty("status").GetString() != "success")
                return null;

            return new LocationInfo
            {
                CountryCode = data.TryGetProperty("countryCode", out var cc) ? cc.GetString() : null,
                CountryName = data.TryGetProperty("country", out var cn) ? cn.GetString() : null,
                Region = data.TryGetProperty("region", out var reg) ? reg.GetString() : null,
                City = data.TryGetProperty("city", out var city) ? city.GetString() : null,
                Timezone = data.TryGetProperty("timezone", out var tz) ? tz.GetString() : null,
                Isp = data.TryGetProperty("isp", out var isp) ? isp.GetString() : null,
                IsProxy = data.TryGetProperty("proxy", out var proxy) && proxy.GetBoolean(),
                IsDataCenter = data.TryGetProperty("hosting", out var hosting) && hosting.GetBoolean(),
                IsVpn = false // ip-api doesn't provide VPN detection in free tier
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling primary geolocation service for IP {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<LocationInfo?> GetLocationFromFallbackServiceAsync(string ipAddress)
    {
        try
        {
            // Using ipinfo.io as fallback (free tier)
            var response = await _httpClient.GetAsync($"https://ipinfo.io/{ipAddress}/json");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new LocationInfo
            {
                CountryCode = data.TryGetProperty("country", out var cc) ? cc.GetString() : null,
                CountryName = null, // ipinfo.io doesn't provide country name
                Region = data.TryGetProperty("region", out var reg) ? reg.GetString() : null,
                City = data.TryGetProperty("city", out var city) ? city.GetString() : null,
                Timezone = data.TryGetProperty("timezone", out var tz) ? tz.GetString() : null,
                Isp = data.TryGetProperty("org", out var org) ? org.GetString() : null,
                IsProxy = false, // ipinfo.io doesn't provide proxy detection in free tier
                IsDataCenter = false,
                IsVpn = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling fallback geolocation service for IP {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<IpGeolocation?> GetCachedLocationAsync(string ipAddress)
    {
        var hash = HashIpAddress(ipAddress);
        return await _context.IpGeolocations
            .FirstOrDefaultAsync(g => g.IpAddressHash == hash);
    }

    private async Task CacheLocationAsync(string ipAddress, LocationInfo locationInfo)
    {
        try
        {
            var hash = HashIpAddress(ipAddress);
            var isRestricted = !string.IsNullOrEmpty(locationInfo.CountryCode) &&
                               IsRestrictedCountry(locationInfo.CountryCode);

            var geolocation = new IpGeolocation
            {
                IpAddressHash = hash,
                CountryCode = locationInfo.CountryCode ?? "UNKNOWN",
                CountryName = locationInfo.CountryName ?? "Unknown",
                City = locationInfo.City,
                Region = locationInfo.Region,
                Timezone = locationInfo.Timezone,
                Isp = locationInfo.Isp,
                IsVpn = locationInfo.IsVpn,
                IsProxy = locationInfo.IsProxy,
                IsDataCenter = locationInfo.IsDataCenter,
                IsRestricted = isRestricted,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Cache for 7 days
            };

            _context.IpGeolocations.Add(geolocation);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching location data for IP {IpAddress}", ipAddress);
        }
    }

    private static string HashIpAddress(string ipAddress)
    {
        // Hash IP address for privacy (GDPR compliance)
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsLocalIpAddress(string ipAddress)
    {
        if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
            return false;

        // Check for localhost
        if (System.Net.IPAddress.IsLoopback(ip))
            return true;

        // Check for private IP ranges
        var bytes = ip.GetAddressBytes();

        // IPv4 private ranges
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Configuration for geolocation services
/// </summary>
public class GeoLocationConfiguration
{
    public string? IpApiKey { get; set; }
    public string? MaxMindLicenseKey { get; set; }
    public int CacheDurationDays { get; set; } = 7;
    public bool EnableVpnDetection { get; set; } = true;
    public bool EnableProxyDetection { get; set; } = true;
}