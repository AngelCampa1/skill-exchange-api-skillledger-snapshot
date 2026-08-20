namespace SkillLedger.Core.Enums;

/// <summary>
/// Notification frequency options for saved searches
/// </summary>
public enum NotificationFrequency
{
    /// <summary>
    /// Send notifications immediately when matching projects are found
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Send daily digest notifications
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Send weekly digest notifications
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Send monthly digest notifications
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// No email notifications
    /// </summary>
    Disabled = 4
}