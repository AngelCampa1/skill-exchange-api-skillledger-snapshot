namespace SkillLedger.Core.Models;

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public bool IsRedisConnected { get; set; }
    public long? RedisDbSize { get; set; }
    public string RedisInfo { get; set; } = string.Empty;
    public int InMemoryCacheSize { get; set; }
}
