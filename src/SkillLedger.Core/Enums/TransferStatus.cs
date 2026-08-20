namespace SkillLedger.Core.Enums;

/// <summary>
/// Status of a credit transfer in the system
/// </summary>
public enum TransferStatus
{
    /// <summary>
    /// Transfer has been initiated but not yet processed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Transfer has been successfully completed
    /// Credits have been moved from sender to recipient
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Transfer failed due to insufficient funds, validation errors, or system issues
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Transfer was reversed after completion (within reversal window)
    /// Credits have been returned to original sender
    /// </summary>
    Reversed = 3,

    /// <summary>
    /// Transfer is being processed (intermediate state)
    /// Used for batch operations or complex transfers
    /// </summary>
    Processing = 4,

    /// <summary>
    /// Transfer has been cancelled before processing
    /// No credits were moved
    /// </summary>
    Cancelled = 5
}