using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace SkillLedger.Api.Controllers;

public class AlertItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string Threshold { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
    public string[] Actions { get; set; } = Array.Empty<string>();
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? MemoryUsage { get; set; }
    public string? Exception { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase, IDisposable
{
    private readonly ILogger<MonitoringController> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource _activitySource;
    private bool _disposed;

    public MonitoringController(
        ILogger<MonitoringController> logger,
        HealthCheckService healthCheckService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _serviceProvider = serviceProvider;
        _activitySource = new ActivitySource("SkillLedger.Monitoring");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _activitySource?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Comprehensive health check with detailed status
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealthStatus([FromQuery] bool detailed = false)
    {
        using var activity = _activitySource.StartActivity("HealthCheck");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            stopwatch.Stop();
            activity?.SetTag("health.status", healthReport.Status.ToString());
            activity?.SetTag("health.duration_ms", stopwatch.ElapsedMilliseconds);

            var response = new
            {
                Status = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Duration = $"{stopwatch.ElapsedMilliseconds}ms",
                Version = GetApplicationVersion()
            };

            if (detailed)
            {
                // Detailed health data requires admin/monitoring role
                if (!User.IsInRole("Admin") && !User.IsInRole("Monitoring"))
                    return Unauthorized(new { error = "Detailed health data requires Admin or Monitoring role" });
                var detailedResults = new
                {
                    response.Status,
                    response.Timestamp,
                    response.Duration,
                    response.Version,
                    Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    Checks = healthReport.Entries.Select(entry => new
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString(),
                        Description = entry.Value.Description,
                        Duration = $"{entry.Value.Duration.TotalMilliseconds}ms",
                        Tags = entry.Value.Tags,
                        Data = entry.Value.Data,
                        Exception = entry.Value.Exception?.Message
                    }),
                    SystemInfo = GetSystemInfo(),
                    PerformanceMetrics = GetPerformanceMetrics()
                };

                return healthReport.Status == HealthStatus.Healthy
                    ? Ok(detailedResults)
                    : StatusCode(StatusCodes.Status503ServiceUnavailable, detailedResults);
            }

            return healthReport.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("health.error", ex.Message);
            activity?.SetTag("health.duration_ms", stopwatch.ElapsedMilliseconds);

            _logger.LogError(ex, "Health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message,
                Duration = $"{stopwatch.ElapsedMilliseconds}ms"
            });
        }
    }

    /// <summary>
    /// Get detailed performance metrics
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Roles = "Admin,Monitoring")]
    public async Task<IActionResult> GetMetrics([FromQuery] string? category = null)
    {
        using var activity = _activitySource.StartActivity("Metrics");

        try
        {
            var metrics = new
            {
                Timestamp = DateTime.UtcNow,
                System = GetSystemInfo(),
                Performance = GetPerformanceMetrics(),
                Memory = GetMemoryMetrics(),
                Process = GetProcessMetrics(),
                Network = GetNetworkMetrics(),
                Cache = await GetCacheMetricsAsync(),
                Database = GetDatabaseMetrics(),
                Application = GetApplicationMetrics()
            };

            activity?.SetTag("metrics.category", category ?? "all");

            if (!string.IsNullOrEmpty(category))
            {
                object categoryMetrics = category.ToLower() switch
                {
                    "system" => new { metrics.System },
                    "performance" => new { metrics.Performance },
                    "memory" => new { metrics.Memory },
                    "process" => new { metrics.Process },
                    "network" => new { metrics.Network },
                    "cache" => new { metrics.Cache },
                    "database" => new { metrics.Database },
                    "application" => new { metrics.Application },
                    _ => metrics
                };
                return Ok(categoryMetrics);
            }

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            activity?.SetTag("metrics.error", ex.Message);
            _logger.LogError(ex, "Error getting metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics" });
        }
    }

    /// <summary>
    /// Get application logs (for monitoring dashboard)
    /// </summary>
    [HttpGet("logs")]
    [Authorize(Roles = "Admin,Monitoring")]
    public IActionResult GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] string? level = null)
    {
        using var activity = _activitySource.StartActivity("Logs");

        try
        {
            // In production, this would query a log aggregation system
            var logs = new LogEntry[]
            {
                new LogEntry
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Level = "Information",
                    Message = "User login successful",
                    Category = "Authentication",
                    UserId = "user123",
                    IpAddress = "192.168.1.100",
                    CorrelationId = Guid.NewGuid().ToString()
                },
                new LogEntry
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-10),
                    Level = "Warning",
                    Message = "High memory usage detected",
                    Category = "Performance",
                    MemoryUsage = "85%",
                    CorrelationId = Guid.NewGuid().ToString()
                },
                new LogEntry
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-15),
                    Level = "Error",
                    Message = "Database connection timeout",
                    Category = "Database",
                    Exception = "TimeoutException: Connection timeout after 30 seconds",
                    CorrelationId = Guid.NewGuid().ToString()
                }
            };

            if (!string.IsNullOrEmpty(level))
            {
                logs = logs.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            var pagedLogs = logs
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            activity?.SetTag("logs.count", pagedLogs.Length);
            activity?.SetTag("logs.level", level ?? "all");

            return Ok(new
            {
                Logs = pagedLogs,
                Page = page,
                PageSize = pageSize,
                TotalCount = logs.Length,
                Filters = new { Level = level }
            });
        }
        catch (Exception ex)
        {
            activity?.SetTag("logs.error", ex.Message);
            _logger.LogError(ex, "Error retrieving logs");
            return StatusCode(500, new { error = "Failed to retrieve logs" });
        }
    }

    /// <summary>
    /// Get application tracing information
    /// </summary>
    [HttpGet("tracing")]
    [Authorize(Roles = "Admin,Monitoring")]
    public IActionResult GetTracing([FromQuery] string? traceId = null, [FromQuery] int hours = 24)
    {
        using var activity = _activitySource.StartActivity("Tracing");

        try
        {
            var traces = new object[]
            {
                new
                {
                    TraceId = traceId ?? Guid.NewGuid().ToString(),
                    SpanId = Guid.NewGuid().ToString(),
                    ParentSpanId = (string?)null,
                    OperationName = "HTTP GET /api/projects",
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    Duration = "150ms",
                    Status = "OK",
                    Tags = new
                    {
                        http_method = "GET",
                        http_status_code = "200",
                        user_id = "user123",
                        service_name = "skillledger-api"
                    },
                    Logs = new object[]
                    {
                        new { Timestamp = DateTime.UtcNow.AddMinutes(-5), Message = "Request started" },
                        new { Timestamp = DateTime.UtcNow.AddMinutes(-4).AddSeconds(850), Message = "Request completed successfully" }
                    }
                },
                new
                {
                    TraceId = Guid.NewGuid().ToString(),
                    SpanId = Guid.NewGuid().ToString(),
                    ParentSpanId = (string?)null,
                    OperationName = "Database.Query",
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    Duration = "45ms",
                    Status = "OK",
                    Tags = new
                    {
                        db_statement = "SELECT * FROM Projects",
                        db_type = "sqlserver",
                        service_name = "skillledger-api"
                    }
                }
            };

            activity?.SetTag("tracing.count", traces.Length);

            return Ok(new
            {
                Traces = traces,
                QueryParameters = new { TraceId = traceId, Hours = hours },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            activity?.SetTag("tracing.error", ex.Message);
            _logger.LogError(ex, "Error retrieving tracing information");
            return StatusCode(500, new { error = "Failed to retrieve tracing information" });
        }
    }

    /// <summary>
    /// Get alert status and configuration
    /// </summary>
    [HttpGet("alerts")]
    [Authorize(Roles = "Admin,Monitoring")]
    public IActionResult GetAlerts([FromQuery] bool activeOnly = true)
    {
        using var activity = _activitySource.StartActivity("Alerts");

        try
        {
            var alerts = new AlertItem[]
            {
                new AlertItem
                {
                    Id = Guid.NewGuid(),
                    Name = "High Response Time",
                    Severity = "Warning",
                    Status = "Active",
                    Condition = "avg response time > 200ms",
                    CurrentValue = "250ms",
                    Threshold = "200ms",
                    TriggeredAt = DateTime.UtcNow.AddMinutes(-15),
                    Description = "API response time is above threshold",
                    AffectedServices = new string[] { "skillledger-api" },
                    Actions = new string[] { "Notify team", "Scale up resources" }
                },
                new AlertItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Memory Usage High",
                    Severity = "Critical",
                    Status = "Resolved",
                    Condition = "memory usage > 90%",
                    CurrentValue = "75%",
                    Threshold = "90%",
                    TriggeredAt = DateTime.UtcNow.AddHours(-2),
                    ResolvedAt = DateTime.UtcNow.AddMinutes(-30),
                    Description = "Memory usage was critically high but has been resolved",
                    AffectedServices = new string[] { "skillledger-api" },
                    Actions = new string[] { "Restarted service", "Increased memory limits" }
                }
            };

            if (activeOnly)
            {
                alerts = alerts.Where(a => a.Status == "Active").ToArray();
            }

            activity?.SetTag("alerts.count", alerts.Length);
            activity?.SetTag("alerts.active_only", activeOnly);

            return Ok(new
            {
                Alerts = alerts,
                Timestamp = DateTime.UtcNow,
                Configuration = new
                {
                    ResponseTimeThreshold = "200ms",
                    MemoryThreshold = "90%",
                    ErrorRateThreshold = "5%",
                    NotificationChannels = new string[] { "Email", "Slack", "PagerDuty" }
                }
            });
        }
        catch (Exception ex)
        {
            activity?.SetTag("alerts.error", ex.Message);
            _logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(500, new { error = "Failed to retrieve alerts" });
        }
    }

    private object GetSystemInfo()
    {
        return new
        {
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            Is64BitProcess = Environment.Is64BitProcess,
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem
        };
    }

    private object GetPerformanceMetrics()
    {
        var process = Process.GetCurrentProcess();
        return new
        {
            CpuUsage = GetCpuUsage(),
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            StartTime = process.StartTime,
            TotalProcessorTime = process.TotalProcessorTime.TotalSeconds,
            PrivilegedProcessorTime = process.PrivilegedProcessorTime.TotalSeconds,
            UserProcessorTime = process.UserProcessorTime.TotalSeconds
        };
    }

    private object GetMemoryMetrics()
    {
        var process = Process.GetCurrentProcess();
        var gcMemoryInfo = GC.GetGCMemoryInfo();

        return new
        {
            ProcessWorkingSet = process.WorkingSet64,
            ProcessPrivateMemory = process.PrivateMemorySize64,
            ProcessVirtualMemory = process.VirtualMemorySize64,
            GcTotalMemory = GC.GetTotalMemory(false),
            GcHeapSize = gcMemoryInfo.HeapSizeBytes,
            GcFragmentedBytes = gcMemoryInfo.FragmentedBytes,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalMemory = gcMemoryInfo.TotalAvailableMemoryBytes
        };
    }

    private object GetProcessMetrics()
    {
        var process = Process.GetCurrentProcess();
        return new
        {
            Id = process.Id,
            ProcessName = process.ProcessName,
            MainModule = process.MainModule?.FileName,
            StartTime = process.StartTime,
            Responding = process.Responding,
            SessionId = process.SessionId
        };
    }

    private object GetNetworkMetrics()
    {
        // In production, this would use network performance counters
        return new
        {
            ActiveConnections = GetActiveConnectionCount(),
            BytesReceived = GetNetworkBytesReceived(),
            BytesSent = GetNetworkBytesSent(),
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> GetCacheMetricsAsync()
    {
        try
        {
            var cacheService = _serviceProvider.GetService(typeof(SkillLedger.Infrastructure.Services.PerformanceOptimizationService))
                as SkillLedger.Infrastructure.Services.PerformanceOptimizationService;

            if (cacheService != null)
            {
                // BUG-013 FIX: Properly await instead of using GetAwaiter().GetResult()
                var stats = await cacheService.GetStatisticsAsync();
                return new
                {
                    IsRedisConnected = stats.IsRedisConnected,
                    RedisDbSize = stats.RedisDbSize,
                    RedisInfo = stats.RedisInfo,
                    InMemoryCacheSize = stats.InMemoryCacheSize
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not get cache metrics");
        }

        return new
        {
            MemoryCacheEnabled = true,
            RedisCacheEnabled = false,
            MemoryCacheEntries = 0,
            RedisUsedMemory = "N/A",
            RedisHitRate = 0,
            RedisHits = 0,
            RedisMisses = 0
        };
    }

    private object GetDatabaseMetrics()
    {
        // In production, this would query database performance counters
        return new
        {
            ActiveConnections = 15,
            ConnectionPoolSize = 100,
            AverageQueryTime = "45ms",
            SlowQueries = 2,
            DatabaseSize = "2.5GB",
            Timestamp = DateTime.UtcNow
        };
    }

    private object GetApplicationMetrics()
    {
        return new
        {
            Version = GetApplicationVersion(),
            Uptime = GetApplicationUptime(),
            RequestCount = GetRequestCount(),
            ErrorCount = GetErrorCount(),
            AverageResponseTime = GetAverageResponseTime(),
            LastRequestTime = DateTime.UtcNow.AddMinutes(-1)
        };
    }

    private string GetApplicationVersion()
    {
        return System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
    }

    private TimeSpan GetApplicationUptime()
    {
        return DateTime.UtcNow - Process.GetCurrentProcess().StartTime;
    }

    private double GetCpuUsage()
    {
        var process = Process.GetCurrentProcess();
        var cpuUsedMs = process.TotalProcessorTime.TotalMilliseconds;
        var totalMsPassed = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds;
        if (totalMsPassed <= 0) return 0.0;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        return Math.Min(cpuUsageTotal * 100.0, 100.0);
    }

    private int GetActiveConnectionCount()
    {
        // BUG-012 FIX: Return placeholder with note instead of random data
        // In production, this would query actual network statistics
        return 0; // Not available without network performance counters
    }

    private long GetNetworkBytesReceived()
    {
        // BUG-012 FIX: Return placeholder with note instead of random data
        // In production, this would query actual network statistics
        return 0; // Not available without network performance counters
    }

    private long GetNetworkBytesSent()
    {
        // BUG-012 FIX: Return placeholder with note instead of random data
        // In production, this would query actual network statistics
        return 0; // Not available without network performance counters
    }

    private long GetRequestCount()
    {
        // BUG-012 FIX: Return placeholder with note instead of random data
        // In production, this would be tracked via middleware/telemetry
        return 0; // Not available without request counter middleware
    }

    private long GetErrorCount()
    {
        // BUG-012 FIX: Return placeholder with note instead of random data
        // In production, this would be tracked via middleware/telemetry
        return 0; // Not available without error tracking middleware
    }

    private string GetAverageResponseTime()
    {
        // BUG-012 FIX: Return placeholder with note instead of random data
        // In production, this would be calculated from telemetry data
        return "N/A"; // Not available without response time tracking middleware
    }
}
