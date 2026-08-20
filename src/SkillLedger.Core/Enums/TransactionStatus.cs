namespace SkillLedger.Core.Enums;

/// <summary>
/// Status values for credit transactions
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction has been created but not yet processed
    /// Initial state for all transactions
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Transaction is currently being processed by the system
    /// Intermediate state during validation and execution
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Transaction has been successfully completed
    /// Credits have been transferred and balances updated
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Transaction failed due to insufficient funds, validation error, or system error
    /// No credits were transferred
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Transaction was cancelled by user or system before completion
    /// Usually during the pending or processing state
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Transaction is under review for potential fraud or policy violations
    /// Processing is suspended pending manual review
    /// </summary>
    UnderReview = 5,

    /// <summary>
    /// Transaction has been disputed by one of the parties
    /// Requires manual resolution by platform support
    /// </summary>
    Disputed = 6,

    /// <summary>
    /// Transaction was reversed due to dispute resolution or refund
    /// Credits have been returned to original sender
    /// </summary>
    Reversed = 7,

    /// <summary>
    /// Transaction expired without completion
    /// Usually for time-sensitive operations like escrow deposits
    /// </summary>
    Expired = 8
}