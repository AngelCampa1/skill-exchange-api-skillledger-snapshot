using Microsoft.Extensions.Primitives;

namespace SkillLedger.Api.Middleware;

/// <summary>
/// BUG-041 FIX: Middleware to generate and propagate correlation IDs across requests
/// This enables tracing of requests through the entire system for debugging and monitoring
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdLogKey = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header
        string correlationId;

        var firstCorrelationId = context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues correlationIds)
            ? correlationIds.FirstOrDefault()
            : null;

        if (!string.IsNullOrWhiteSpace(firstCorrelationId))
        {
            // Use existing correlation ID from client
            correlationId = firstCorrelationId;
            _logger.LogDebug("Using existing correlation ID from request: {CorrelationId}", correlationId);
        }
        else
        {
            // Generate new correlation ID
            correlationId = Guid.NewGuid().ToString();
            _logger.LogDebug("Generated new correlation ID: {CorrelationId}", correlationId);
        }

        // Add correlation ID to response headers for client reference
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // Add correlation ID to HttpContext items for use in controllers and services
        context.Items[CorrelationIdLogKey] = correlationId;

        // Add correlation ID to logging context (works with Serilog, NLog, etc.)
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            [CorrelationIdLogKey] = correlationId
        }))
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log error with correlation ID for easier troubleshooting
                _logger.LogError(ex,
                    "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                    correlationId, context.Request.Path, context.Request.Method);
                throw;
            }
        }
    }
}

/// <summary>
/// Extension methods for CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the correlation ID middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Helper method to get correlation ID from HttpContext
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items.TryGetValue("CorrelationId", out var correlationId)
            ? correlationId?.ToString()
            : null;
    }
}

