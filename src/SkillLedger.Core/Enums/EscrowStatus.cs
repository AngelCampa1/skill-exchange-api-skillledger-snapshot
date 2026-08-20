namespace SkillLedger.Core.Enums;

/// <summary>
/// Status values for project escrow accounts
/// </summary>
public enum EscrowStatus
{
    /// <summary>
    /// Escrow account is active and holding funds
    /// Credits are secured and available for milestone releases
    /// </summary>
    Active = 0,

    /// <summary>
    /// Escrow has been fully released to the provider
    /// All milestones completed and project finished successfully
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Escrow is in dispute and frozen
    /// Requires admin intervention to resolve
    /// </summary>
    Disputed = 2,

    /// <summary>
    /// Escrow has been cancelled and funds returned to client
    /// Project was cancelled before completion
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Escrow is partially released (some milestones complete)
    /// Used for milestone-based payment systems
    /// </summary>
    PartiallyReleased = 4,

    /// <summary>
    /// Escrow is frozen due to security or policy violations
    /// Requires admin review before any releases can occur
    /// </summary>
    Frozen = 5
}