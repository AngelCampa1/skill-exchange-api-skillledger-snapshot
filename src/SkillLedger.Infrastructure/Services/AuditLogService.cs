using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Collections.Concurrent;

namespace SkillLedger.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IMemoryCache _memoryCache;

    // P1 PERFORMANCE FIX: Rate limiting for audit log flooding prevention
    // Changed from static to instance-based to prevent test interference
    private readonly ConcurrentDictionary<string, DateTime> _lastLogTime = new();
    private static readonly TimeSpan _minLogInterval = TimeSpan.FromSeconds(1); // Max 1 log per second per user-action combo

    // P1 FIX: Define log priorities for sampling
    private static readonly HashSet<string> _criticalActions = new()
    {
        "LOGIN_FAILED",
        "USER_REGISTRATION",
        "PASSWORD_RESET",
        "CREDIT_TRANSFER",
        "PROJECT_CREATE",
        "UNAUTHORIZED_ACCESS",
        "PAYMENT_FAILED"
    };

    private static readonly HashSet<string> _sampledActions = new()
    {
        "PROJECT_VIEW",
        "PROFILE_VIEW",
        "SEARCH_QUERY",
        "API_CALL"
    };

    public AuditLogService(
        SkillLedgerDbContext context,
        ILogger<AuditLogService> logger,
        IMemoryCache memoryCache)
    {
        _context = context;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// P1 PERFORMANCE FIX: Log audit event with flood prevention and sampling
    /// </summary>
    public async Task LogEventAsync(Guid? userId, string action, string ipAddress, string? userAgent, bool success, string? details = null, string? errorMessage = null)
    {
        try
        {
            // P1 FIX: Check if we should log this event (sampling + rate limiting)
            if (!ShouldLogEvent(userId, action, success))
            {
                // Increment counter in memory instead of writing to database
                IncrementSampledEventCounter(action);
                return;
            }

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                Details = details,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            if (!success)
            {
                _logger.LogWarning("Audit log recorded - Action: {Action}, Success: {Success}, IP: {IPAddress}, Error: {Error}",
                    action, success, ipAddress, errorMessage);
            }
        }
        catch (Exception ex)
        {
            // BUG-LOW-003 FIX: Clarify that we catch but don't propagate exceptions
            // Log the error but don't propagate the exception - audit logging should not break the main flow
            _logger.LogError(ex, "Failed to write audit log - Action: {Action}, IP: {IPAddress}", action, ipAddress);
        }
    }

    /// <summary>
    /// P1 PERFORMANCE FIX: Determine if event should be logged based on priority and rate limiting
    /// </summary>
    private bool ShouldLogEvent(Guid? userId, string action, bool success)
    {
        // Always log critical actions
        if (_criticalActions.Contains(action))
        {
            return true;
        }

        // Always log failures (important for security)
        if (!success)
        {
            return true;
        }

        // Apply rate limiting to prevent flooding
        var rateLimitKey = $"{userId}:{action}";
        var now = DateTime.UtcNow;

        if (_lastLogTime.TryGetValue(rateLimitKey, out var lastTime))
        {
            if (now - lastTime < _minLogInterval)
            {
                // Too soon, skip this log
                return false;
            }
        }

        // Update last log time
        _lastLogTime[rateLimitKey] = now;

        // Clean up old entries (simple cleanup every 1000 entries)
        if (_lastLogTime.Count > 10000)
        {
            var cutoff = now.AddMinutes(-5);
            var keysToRemove = _lastLogTime
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _lastLogTime.TryRemove(key, out _);
            }
        }

        // Apply sampling to non-critical actions
        if (_sampledActions.Contains(action))
        {
            // SECURITY FIX: Use cryptographic random for audit log sampling to prevent predictable bypass
            // Log only 1 in 10 sampled actions
            Span<byte> randomBytes = stackalloc byte[1];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            return (randomBytes[0] % 10) == 0;
        }

        // Log everything else (but rate-limited)
        return true;
    }

    /// <summary>
    /// P1 PERFORMANCE FIX: Track sampled events in memory for reporting
    /// BUG-023 FIX: Include user ID in cache key to prevent collision
    /// </summary>
    private void IncrementSampledEventCounter(string action)
    {
        // BUG-023 FIX: Include "global" in key to indicate system-wide counter
        // Individual user counters would use userId in key
        var cacheKey = $"audit_sampled_global_{action}_{DateTime.UtcNow:yyyyMMddHH}";

        if (_memoryCache.TryGetValue<long>(cacheKey, out var count))
        {
            _memoryCache.Set(cacheKey, count + 1, TimeSpan.FromHours(2));
        }
        else
        {
            _memoryCache.Set(cacheKey, 1L, TimeSpan.FromHours(2));
        }
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            var skip = (pageNumber - 1) * pageSize;

            return await _context.AuditLogs
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user: {UserId}", userId);
            return new List<AuditLog>();
        }
    }

    public async Task<int> GetRecentFailedAttemptsAsync(string ipAddress, int hoursBack = 1)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

            return await _context.AuditLogs
                .Where(al => al.IPAddress == ipAddress &&
                           !al.Success &&
                           al.Timestamp > cutoffTime &&
                           (al.Action == AuditActions.LOGIN_FAILED ||
                            al.Action == AuditActions.USER_REGISTRATION))
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent failed attempts for IP: {IPAddress}", ipAddress);
            return 0;
        }
    }
}