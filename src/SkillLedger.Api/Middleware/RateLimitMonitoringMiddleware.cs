using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;

namespace SkillLedger.Api.Middleware;

/// <summary>
/// P1 SECURITY FIX: Monitor and log rate limiting events
/// This middleware tracks rate limit violations for security monitoring
/// </summary>
public class RateLimitMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMonitoringMiddleware> _logger;

    // LOW-PRIORITY FIX: Track repeated violations for security alerting
    private static readonly Dictionary<string, List<DateTime>> _violationTracking = new();
    private static readonly object _lock = new();
    private const int MaxViolationsBeforeAlert = 10;
    private const int ViolationWindowMinutes = 5;

    public RateLimitMonitoringMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Check if request was rate limited
        var rateLimitPartition = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
        var policyName = rateLimitPartition?.PolicyName;

        try
        {
            await _next(context);

            stopwatch.Stop();

            // Log rate limit rejections (429 status code)
            if (context.Response.StatusCode == 429)
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = context.Request.Headers.UserAgent.ToString();
                var endpoint = $"{context.Request.Method} {context.Request.Path}";

                _logger.LogWarning(
                    "RATE_LIMIT_EXCEEDED: IP={IpAddress}, Endpoint={Endpoint}, Policy={Policy}, UserAgent={UserAgent}, Duration={Duration}ms",
                    ipAddress,
                    endpoint,
                    policyName ?? "Unknown",
                    userAgent,
                    stopwatch.ElapsedMilliseconds);

                // LOW-PRIORITY FIX: Implement security alert for repeated violations
                TrackViolationAndAlert(ipAddress, endpoint);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "REQUEST_ERROR: Path={Path}, Policy={Policy}, Duration={Duration}ms",
                context.Request.Path,
                policyName ?? "None",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// BUG-BE-002 FIX: Track violations and trigger security alerts for repeated offenders
    /// Fixed race condition by using immutable operations instead of RemoveAll
    /// </summary>
    private void TrackViolationAndAlert(string ipAddress, string endpoint)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var cutoffTime = now.AddMinutes(-ViolationWindowMinutes);

            // BUG-BE-002 FIX: Use immutable operations to prevent collection modification during enumeration
            // Get existing violations or empty list
            var existingViolations = _violationTracking.ContainsKey(ipAddress)
                ? _violationTracking[ipAddress]
                : new List<DateTime>();

            // Create new filtered list instead of modifying in place (prevents RemoveAll race condition)
            var filteredViolations = existingViolations.Where(v => v >= cutoffTime).ToList();

            // Add current violation to the new list
            filteredViolations.Add(now);

            // Check if threshold exceeded
            if (filteredViolations.Count >= MaxViolationsBeforeAlert)
            {
                _logger.LogError(
                    "SECURITY_ALERT: Excessive rate limit violations detected. " +
                    "IP={IpAddress}, Violations={Count} in {Minutes} minutes, LastEndpoint={Endpoint}. " +
                    "Consider blocking this IP address.",
                    ipAddress,
                    filteredViolations.Count,
                    ViolationWindowMinutes,
                    endpoint);

                // Reset counter after alert to avoid spam
                filteredViolations.Clear();
            }

            // Update dictionary with new list (atomic operation)
            _violationTracking[ipAddress] = filteredViolations;

            // Cleanup: Remove IPs with no recent violations (separate loop to avoid enumeration issues)
            var ipsToRemove = _violationTracking.Keys
                .Where(ip => _violationTracking[ip].Count == 0 || _violationTracking[ip].All(v => v < cutoffTime))
                .ToList();

            foreach (var ip in ipsToRemove)
            {
                _violationTracking.Remove(ip);
            }
        }
    }
}

/// <summary>
/// Extension method to add rate limit monitoring middleware
/// </summary>
public static class RateLimitMonitoringMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimitMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMonitoringMiddleware>();
    }
}

