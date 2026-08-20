namespace SkillLedger.Core.Interfaces;

public interface ICdnService
{
    /// <summary>
    /// Uploads a file to CDN for global distribution
    /// </summary>
    Task<string> UploadToCdnAsync(Stream fileStream, string fileName, string mimeType);

    /// <summary>
    /// Gets optimized CDN URL for a file
    /// </summary>
    Task<string> GetCdnUrlAsync(string filePath, CdnOptions? options = null);

    /// <summary>
    /// Invalidates CDN cache for a file
    /// </summary>
    Task<bool> InvalidateCacheAsync(string filePath);

    /// <summary>
    /// Purges file from CDN
    /// </summary>
    Task<bool> PurgeFromCdnAsync(string filePath);

    /// <summary>
    /// Gets CDN statistics for a file
    /// </summary>
    Task<CdnStatistics> GetStatisticsAsync(string filePath);
}

public class CdnOptions
{
    public bool EnableCompression { get; set; } = true;
    public int CacheDurationMinutes { get; set; } = 1440; // 24 hours
    public string? CustomDomain { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class CdnStatistics
{
    public string FilePath { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long TotalBandwidth { get; set; }
    public double CacheHitRatio { get; set; }
    public DateTime LastAccessed { get; set; }
    public string[] EdgeLocations { get; set; } = Array.Empty<string>();
}