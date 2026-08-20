using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Logs an audit event
    /// </summary>
    /// <param name="userId">User ID (nullable for system actions)</param>
    /// <param name="action">Action type</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <param name="success">Whether the action was successful</param>
    /// <param name="details">Additional details (JSON format)</param>
    /// <param name="errorMessage">Error message if failed</param>
    Task LogEventAsync(Guid? userId, string action, string ipAddress, string? userAgent, bool success, string? details = null, string? errorMessage = null);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <returns>List of audit logs</returns>
    Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Gets recent failed authentication attempts for security monitoring
    /// </summary>
    /// <param name="ipAddress">IP address to check</param>
    /// <param name="hoursBack">Number of hours to look back</param>
    /// <returns>Number of failed attempts</returns>
    Task<int> GetRecentFailedAttemptsAsync(string ipAddress, int hoursBack = 1);
}

/// <summary>
/// Audit action constants
/// </summary>
public static class AuditActions
{
    // Authentication & Account Actions
    public const string USER_REGISTRATION = "USER_REGISTRATION";
    public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
    public const string LOGIN_FAILED = "LOGIN_FAILED";
    public const string PASSWORD_CHANGE = "PASSWORD_CHANGE";
    public const string ACCOUNT_LOCKOUT = "ACCOUNT_LOCKOUT";
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    // Milestone Actions
    public const string MILESTONE_CREATED = "MILESTONE_CREATED";
    public const string MILESTONE_UPDATED = "MILESTONE_UPDATED";
    public const string MILESTONE_DELETED = "MILESTONE_DELETED";
    public const string MILESTONE_STARTED = "MILESTONE_STARTED";
    public const string MILESTONE_SUBMITTED = "MILESTONE_SUBMITTED";
    public const string MILESTONE_APPROVED = "MILESTONE_APPROVED";
    public const string MILESTONE_REVISION_REQUESTED = "MILESTONE_REVISION_REQUESTED";
    public const string MILESTONE_CANCELLED = "MILESTONE_CANCELLED";
    public const string SUBMISSION_CREATED = "SUBMISSION_CREATED";
    public const string SUBMISSION_REVIEWED = "SUBMISSION_REVIEWED";
    public const string ESCROW_LINKED = "ESCROW_LINKED";
    public const string PAYMENT_RELEASE_TRIGGERED = "PAYMENT_RELEASE_TRIGGERED";

    // Messaging Actions
    public const string MESSAGE_SENT = "MESSAGE_SENT";
    public const string MESSAGE_EDITED = "MESSAGE_EDITED";
    public const string MESSAGE_DELETED = "MESSAGE_DELETED";
    public const string MESSAGE_READ = "MESSAGE_READ";
    public const string MESSAGES_READ_ALL = "MESSAGES_READ_ALL";
    public const string REACTION_ADDED = "REACTION_ADDED";
    public const string REACTION_REMOVED = "REACTION_REMOVED";
}