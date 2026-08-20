namespace SkillLedger.Core.Enums;

/// <summary>
/// Types of credit transactions in the SkillLedger platform
/// </summary>
public enum CreditTransactionType
{
    /// <summary>
    /// Initial credits awarded to new verified users (one-time, 100 credits)
    /// </summary>
    StartingCredit = 0,

    /// <summary>
    /// Credits deposited into escrow for a project
    /// Held until project completion or cancellation
    /// </summary>
    EscrowDeposit = 1,

    /// <summary>
    /// Credits released from escrow to service provider upon project completion
    /// </summary>
    EscrowRelease = 2,

    /// <summary>
    /// Credits returned from escrow to client (project cancellation or dispute)
    /// </summary>
    EscrowRefund = 3,

    /// <summary>
    /// Direct payment from client to service provider
    /// For services outside of escrow system
    /// </summary>
    DirectPayment = 4,

    /// <summary>
    /// Payment for completed project work
    /// Standard payment flow for project completion
    /// </summary>
    ProjectPayment = 5,

    /// <summary>
    /// Bonus payment for exceptional work
    /// Additional credits beyond agreed project amount
    /// </summary>
    BonusPayment = 6,

    /// <summary>
    /// Refund for cancelled or disputed services
    /// Credits returned to original sender
    /// </summary>
    Refund = 7,

    /// <summary>
    /// Credits purchased from the platform
    /// Conversion from fiat currency to platform credits
    /// </summary>
    Purchase = 8,

    /// <summary>
    /// Platform fee deduction for services
    /// Administrative costs taken by platform
    /// </summary>
    PlatformFee = 9,

    /// <summary>
    /// Penalty deduction for policy violations
    /// Credits deducted due to user misconduct
    /// </summary>
    Penalty = 10,

    /// <summary>
    /// Credits awarded for referrals, promotions, or platform rewards
    /// Marketing and incentive programs
    /// </summary>
    Reward = 11,

    /// <summary>
    /// Administrative adjustment by platform staff
    /// Manual corrections or support resolutions
    /// </summary>
    Adjustment = 12
}