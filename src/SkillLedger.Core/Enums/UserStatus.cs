namespace SkillLedger.Core.Enums;

public enum UserStatus
{
    /// <summary>
    /// User is registered and active
    /// </summary>
    Active = 0,

    /// <summary>
    /// Phone verified, not tax compliant
    /// </summary>
    PhoneVerified = 1,

    /// <summary>
    /// Fully verified and tax compliant
    /// </summary>
    TaxCompliant = 2,

    /// <summary>
    /// Account suspended
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Account banned
    /// </summary>
    Banned = 4
}