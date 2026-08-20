using System.Diagnostics;

namespace SkillLedger.Api.Middleware;

/// <summary>
/// P1 PERFORMANCE FIX: Middleware to enforce request timeouts and prevent hanging requests
/// </summary>
public class RequestTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimeoutMiddleware> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _uploadTimeout = TimeSpan.FromMinutes(5); // Longer timeout for file uploads

    private static readonly HashSet<string> _uploadEndpoints = new()
    {
        "/api/profile/photo",
        "/api/project/attachments",
        "/api/upload"
    };

    public RequestTimeoutMiddleware(
        RequestDelegate next,
        ILogger<RequestTimeoutMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Determine appropriate timeout based on endpoint
        var timeout = IsUploadEndpoint(context.Request.Path)
            ? _uploadTimeout
            : _defaultTimeout;

        // Create cancellation token with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        cts.CancelAfter(timeout);

        try
        {
            // Execute request with timeout
            await _next(context);

            stopwatch.Stop();

            // Log slow requests (>5 seconds)
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
            {
                _logger.LogWarning(
                    "SLOW_REQUEST: {Method} {Path} took {Duration}ms (Timeout: {Timeout}ms)",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    timeout.TotalMilliseconds);
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "REQUEST_TIMEOUT: {Method} {Path} exceeded {Timeout}s timeout",
                context.Request.Method,
                context.Request.Path,
                timeout.TotalSeconds);

            // Return 408 Request Timeout
            context.Response.StatusCode = 408;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Request Timeout",
                message = $"The request exceeded the maximum allowed time of {timeout.TotalSeconds} seconds.",
                statusCode = 408
            });
        }
    }

    private static bool IsUploadEndpoint(PathString path)
    {
        return _uploadEndpoints.Any(endpoint => path.StartsWithSegments(endpoint));
    }
}

/// <summary>
/// Extension method to add request timeout middleware
/// </summary>
public static class RequestTimeoutMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTimeout(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTimeoutMiddleware>();
    }
}

