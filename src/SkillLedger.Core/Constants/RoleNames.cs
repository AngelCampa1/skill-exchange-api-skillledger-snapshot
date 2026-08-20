namespace SkillLedger.Core.Constants;

/// <summary>
/// Standard role names used throughout the application
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// System administrator with full access
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Standard authenticated user
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Moderator with elevated permissions
    /// </summary>
    public const string Moderator = "Moderator";

    /// <summary>
    /// Support staff with limited administrative access
    /// </summary>
    public const string Support = "Support";

    /// <summary>
    /// Read-only analyst role for reporting and analytics
    /// </summary>
    public const string Analyst = "Analyst";

    /// <summary>
    /// All standard roles in order of priority (highest to lowest)
    /// </summary>
    public static readonly string[] All = { Admin, Moderator, Support, Analyst, User };

    /// <summary>
    /// System roles that cannot be deleted or significantly modified
    /// </summary>
    public static readonly string[] SystemRoles = { Admin, User };
}