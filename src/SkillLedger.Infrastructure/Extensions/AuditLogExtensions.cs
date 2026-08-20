using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Extensions;

/// <summary>
/// Extension methods for fire-and-forget audit logging
/// Prevents audit logging failures from blocking HTTP responses
/// </summary>
public static class AuditLogExtensions
{
    /// <summary>
    /// Logs an audit event asynchronously without blocking the caller
    /// Uses fire-and-forget pattern with error suppression
    /// </summary>
    /// <param name="auditLogService">The audit log service</param>
    /// <param name="logger">Logger for capturing audit logging failures</param>
    /// <param name="userId">The user ID performing the action (null for anonymous)</param>
    /// <param name="action">The action being performed (e.g., "PAYMENT_RELEASE_TRIGGERED")</param>
    /// <param name="ipAddress">The client IP address</param>
    /// <param name="userAgent">The client user agent string</param>
    /// <param name="success">Whether the action succeeded</param>
    /// <param name="details">Optional JSON details about the action</param>
    /// <param name="errorMessage">Optional error message if action failed</param>
    public static void LogAuditEventAsync(
        this IAuditLogService auditLogService,
        ILogger logger,
        Guid? userId,
        string action,
        string ipAddress,
        string? userAgent,
        bool success,
        string? details = null,
        string? errorMessage = null)
    {
        // Fire-and-forget: don't await, don't block HTTP response
        _ = Task.Run(async () =>
        {
            try
            {
                await auditLogService.LogEventAsync(
                    userId,
                    action,
                    ipAddress,
                    userAgent,
                    success,
                    details,
                    errorMessage);
            }
            catch (Exception ex)
            {
                // Suppress audit logging errors - don't let them break the request
                logger.LogWarning(ex,
                    "Failed to log audit event {Action} for user {UserId}. " +
                    "Audit logging failure should not block HTTP response.",
                    action,
                    userId);
            }
        });
    }
}
