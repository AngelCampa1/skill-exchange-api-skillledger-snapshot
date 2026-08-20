namespace SkillLedger.Core.Constants;

/// <summary>
/// Constants for audit log action types
/// </summary>
public static class AuditActions
{
    // User Management
    public const string USER_REGISTRATION = "USER_REGISTRATION";
    public const string USER_REGISTRATION_FAILED = "USER_REGISTRATION_FAILED";
    public const string USER_LOGIN = "USER_LOGIN";
    public const string USER_LOGIN_FAILED = "USER_LOGIN_FAILED";
    public const string USER_LOGOUT = "USER_LOGOUT";
    public const string USER_LOCKOUT = "USER_LOCKOUT";
    public const string USER_UNLOCK = "USER_UNLOCK";
    public const string USER_PROFILE_UPDATE = "USER_PROFILE_UPDATE";
    public const string USER_PASSWORD_CHANGE = "USER_PASSWORD_CHANGE";
    public const string USER_PASSWORD_RESET = "USER_PASSWORD_RESET";
    public const string USER_DEACTIVATION = "USER_DEACTIVATION";
    public const string USER_REACTIVATION = "USER_REACTIVATION";

    // Phone Verification
    public const string PHONE_VERIFICATION_SENT = "PHONE_VERIFICATION_SENT";
    public const string PHONE_VERIFICATION_SEND_FAILED = "PHONE_VERIFICATION_SEND_FAILED";
    public const string PHONE_VERIFIED = "PHONE_VERIFIED";
    public const string PHONE_VERIFICATION_FAILED = "PHONE_VERIFICATION_FAILED";
    public const string PHONE_VERIFICATION_EXPIRED = "PHONE_VERIFICATION_EXPIRED";
    public const string PHONE_VERIFICATION_RESENT = "PHONE_VERIFICATION_RESENT";

    // Session Management (Cookie-based Authentication)
    public const string SESSION_CREATED = "SESSION_CREATED";
    public const string SESSION_CREATION_FAILED = "SESSION_CREATION_FAILED";
    public const string SESSION_REFRESHED = "SESSION_REFRESHED";
    public const string SESSION_REFRESH_FAILED = "SESSION_REFRESH_FAILED";
    public const string SESSION_REVOKED = "SESSION_REVOKED";
    public const string SESSION_REVOCATION_FAILED = "SESSION_REVOCATION_FAILED";
    public const string SESSION_VALIDATED = "SESSION_VALIDATED";
    public const string SESSION_VALIDATION_FAILED = "SESSION_VALIDATION_FAILED";

    // Legacy JWT constants (deprecated - kept for backward compatibility)
    [Obsolete("Use SESSION_CREATED instead")]
    public const string JWT_ACCESS_TOKEN_GENERATED = "SESSION_CREATED";
    [Obsolete("Use SESSION_CREATION_FAILED instead")]
    public const string JWT_ACCESS_TOKEN_GENERATION_FAILED = "SESSION_CREATION_FAILED";
    [Obsolete("Use SESSION_REFRESHED instead")]
    public const string JWT_REFRESH_TOKEN_GENERATED = "SESSION_REFRESHED";
    [Obsolete("Use SESSION_CREATION_FAILED instead")]
    public const string JWT_REFRESH_TOKEN_GENERATION_FAILED = "SESSION_CREATION_FAILED";
    [Obsolete("Use SESSION_REFRESHED instead")]
    public const string JWT_TOKEN_REFRESHED = "SESSION_REFRESHED";
    [Obsolete("Use SESSION_REFRESH_FAILED instead")]
    public const string JWT_TOKEN_REFRESH_FAILED = "SESSION_REFRESH_FAILED";
    [Obsolete("Use SESSION_REVOKED instead")]
    public const string JWT_TOKEN_REVOKED = "SESSION_REVOKED";
    [Obsolete("Use SESSION_REVOCATION_FAILED instead")]
    public const string JWT_TOKEN_REVOCATION_FAILED = "SESSION_REVOCATION_FAILED";
    [Obsolete("Use SESSION_VALIDATED instead")]
    public const string JWT_TOKEN_VALIDATED = "SESSION_VALIDATED";
    [Obsolete("Use SESSION_VALIDATION_FAILED instead")]
    public const string JWT_TOKEN_VALIDATION_FAILED = "SESSION_VALIDATION_FAILED";

    // Security Events
    public const string SECURITY_SUSPICIOUS_ACTIVITY = "SECURITY_SUSPICIOUS_ACTIVITY";
    public const string SECURITY_RATE_LIMIT_EXCEEDED = "SECURITY_RATE_LIMIT_EXCEEDED";
    public const string SECURITY_BRUTE_FORCE_ATTEMPT = "SECURITY_BRUTE_FORCE_ATTEMPT";
    public const string SECURITY_IP_BLOCKED = "SECURITY_IP_BLOCKED";
    public const string SECURITY_IP_UNBLOCKED = "SECURITY_IP_UNBLOCKED";
    public const string SECURITY_UNAUTHORIZED_ACCESS = "SECURITY_UNAUTHORIZED_ACCESS";

    // System Events
    public const string SYSTEM_STARTUP = "SYSTEM_STARTUP";
    public const string SYSTEM_SHUTDOWN = "SYSTEM_SHUTDOWN";
    public const string SYSTEM_ERROR = "SYSTEM_ERROR";
    public const string SYSTEM_CONFIGURATION_CHANGE = "SYSTEM_CONFIGURATION_CHANGE";
    public const string SYSTEM_MAINTENANCE_START = "SYSTEM_MAINTENANCE_START";
    public const string SYSTEM_MAINTENANCE_END = "SYSTEM_MAINTENANCE_END";

    // Data Events
    public const string DATA_CREATED = "DATA_CREATED";
    public const string DATA_UPDATED = "DATA_UPDATED";
    public const string DATA_DELETED = "DATA_DELETED";
    public const string DATA_ACCESSED = "DATA_ACCESSED";
    public const string DATA_EXPORTED = "DATA_EXPORTED";
    public const string DATA_IMPORTED = "DATA_IMPORTED";

    // Project Management
    public const string PROJECT_CREATE = "PROJECT_CREATE";
    public const string PROJECT_UPDATE = "PROJECT_UPDATE";
    public const string PROJECT_DELETE = "PROJECT_DELETE";
    public const string PROJECT_PUBLISH = "PROJECT_PUBLISH";
    public const string PROJECT_DRAFT_SAVE = "PROJECT_DRAFT_SAVE";
    public const string PROJECT_MODERATE = "PROJECT_MODERATE";
    public const string PROJECT_VIEW = "PROJECT_VIEW";
    public const string PROJECT_SEARCH = "PROJECT_SEARCH";

    // Project Escrow Management
    public const string ESCROW_CREATED = "ESCROW_CREATED";
    public const string ESCROW_CREATION_FAILED = "ESCROW_CREATION_FAILED";
    public const string ESCROW_MILESTONE_ADDED = "ESCROW_MILESTONE_ADDED";
    public const string ESCROW_MILESTONE_RELEASED = "ESCROW_MILESTONE_RELEASED";
    public const string ESCROW_MILESTONE_RELEASE_FAILED = "ESCROW_MILESTONE_RELEASE_FAILED";
    public const string ESCROW_FULL_RELEASE = "ESCROW_FULL_RELEASE";
    public const string ESCROW_FULL_RELEASE_FAILED = "ESCROW_FULL_RELEASE_FAILED";
    public const string ESCROW_CANCELLED = "ESCROW_CANCELLED";
    public const string ESCROW_CANCELLATION_FAILED = "ESCROW_CANCELLATION_FAILED";
    public const string ESCROW_DISPUTE_RAISED = "ESCROW_DISPUTE_RAISED";
    public const string ESCROW_DISPUTE_RESOLVED = "ESCROW_DISPUTE_RESOLVED";
    public const string ESCROW_FROZEN = "ESCROW_FROZEN";
    public const string ESCROW_UNFROZEN = "ESCROW_UNFROZEN";
    public const string ESCROW_INTEGRITY_CHECK = "ESCROW_INTEGRITY_CHECK";
    public const string ESCROW_INTEGRITY_VIOLATION = "ESCROW_INTEGRITY_VIOLATION";

    // Credit Wallet Operations
    public const string CREDIT_WALLET_CREATED = "CREDIT_WALLET_CREATED";
    public const string CREDIT_TRANSACTION_CREATED = "CREDIT_TRANSACTION_CREATED";
    public const string CREDIT_TRANSACTION_COMPLETED = "CREDIT_TRANSACTION_COMPLETED";
    public const string CREDIT_TRANSACTION_FAILED = "CREDIT_TRANSACTION_FAILED";
    public const string CREDIT_ESCROW_DEPOSIT = "CREDIT_ESCROW_DEPOSIT";
    public const string CREDIT_ESCROW_RELEASE = "CREDIT_ESCROW_RELEASE";
    public const string CREDIT_ESCROW_REFUND = "CREDIT_ESCROW_REFUND";

    // Administrative Events
    public const string ADMIN_USER_CREATED = "ADMIN_USER_CREATED";
    public const string ADMIN_USER_MODIFIED = "ADMIN_USER_MODIFIED";
    public const string ADMIN_USER_DELETED = "ADMIN_USER_DELETED";
    public const string ADMIN_ROLE_ASSIGNED = "ADMIN_ROLE_ASSIGNED";
    public const string ADMIN_ROLE_REMOVED = "ADMIN_ROLE_REMOVED";
    public const string ADMIN_PERMISSION_GRANTED = "ADMIN_PERMISSION_GRANTED";
    public const string ADMIN_PERMISSION_REVOKED = "ADMIN_PERMISSION_REVOKED";

    // Document Management
    public const string DOCUMENT_UPLOADED = "DOCUMENT_UPLOADED";
    public const string DOCUMENT_UPDATED = "DOCUMENT_UPDATED";
    public const string DOCUMENT_DELETED = "DOCUMENT_DELETED";
    public const string DOCUMENT_SHARED = "DOCUMENT_SHARED";
    public const string DOCUMENT_UNSHARED = "DOCUMENT_UNSHARED";
    public const string DOCUMENT_DOWNLOADED = "DOCUMENT_DOWNLOADED";
    public const string DOCUMENT_VERSION_CREATED = "DOCUMENT_VERSION_CREATED";
    public const string DOCUMENT_PERMISSION_CHANGED = "DOCUMENT_PERMISSION_CHANGED";

    // FileShare Management
    public const string FILE_UPLOADED = "FILE_UPLOADED";
    public const string FILE_DELETED = "FILE_DELETED";
    public const string FILE_SHARED = "FILE_SHARED";
    public const string FILE_UNSHARED = "FILE_UNSHARED";
    public const string FILE_DOWNLOADED = "FILE_DOWNLOADED";
    public const string FILE_PERMISSION_CHANGED = "FILE_PERMISSION_CHANGED";
    public const string FILE_MOVED = "FILE_MOVED";
    public const string FILE_RENAMED = "FILE_RENAMED";
}