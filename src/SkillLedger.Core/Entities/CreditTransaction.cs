using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Immutable transaction record for credit transfers with cryptographic integrity
/// Implements blockchain-inspired immutable ledger with tamper detection
/// </summary>
public class CreditTransaction
{
    public CreditTransaction()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = TransactionStatus.Pending;
    }

    /// <summary>
    /// Unique identifier for the transaction
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID of the credit sender (null for system transactions like starting credits)
    /// </summary>
    public Guid? FromUserId { get; set; }

    /// <summary>
    /// Navigation property to the sender
    /// </summary>
    public User? FromUser { get; set; }

    /// <summary>
    /// User ID of the credit recipient (null for system deductions)
    /// </summary>
    public Guid? ToUserId { get; set; }

    /// <summary>
    /// Navigation property to the recipient
    /// </summary>
    public User? ToUser { get; set; }

    /// <summary>
    /// Amount of credits being transferred
    /// Must be positive (enforced by database constraint)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Transaction amount must be positive")]
    public int Amount { get; set; }

    /// <summary>
    /// Type of transaction (starting credit, escrow, payment, etc.)
    /// </summary>
    public CreditTransactionType Type { get; set; }

    /// <summary>
    /// Current status of the transaction
    /// </summary>
    public TransactionStatus Status { get; set; }

    /// <summary>
    /// Optional reference to related project (for project-related transactions)
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Navigation property to related project
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// Human-readable description of the transaction
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Cryptographic hash for tamper detection and integrity verification
    /// Calculated from transaction data using HMAC-SHA256
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the previous transaction for chain integrity (blockchain-inspired)
    /// </summary>
    [MaxLength(128)]
    public string? PreviousTransactionHash { get; set; }

    /// <summary>
    /// When the transaction was created
    /// Immutable timestamp for audit trail
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the transaction was completed (null if still pending/processing)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the transaction failed (null if not failed)
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Reason for failure if transaction failed
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// IP address from which the transaction was initiated
    /// For fraud detection and audit purposes
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? InitiatedFromIP { get; set; }

    /// <summary>
    /// User agent of the client that initiated the transaction
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata about the transaction (JSON format)
    /// Used for fraud analysis and business intelligence
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this transaction has been flagged for review
    /// </summary>
    public bool IsFlagged { get; set; } = false;

    /// <summary>
    /// Reason for flagging if transaction is flagged
    /// </summary>
    [MaxLength(500)]
    public string? FlaggedReason { get; set; }

    /// <summary>
    /// When the transaction was flagged
    /// </summary>
    public DateTime? FlaggedAt { get; set; }

    // Note: Wallet navigation properties are accessed via service layer queries
    // due to nullable foreign key complexity

    /// <summary>
    /// Mark transaction as completed
    /// </summary>
    public void Complete()
    {
        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        FailedAt = null;
        FailureReason = null;
    }

    /// <summary>
    /// Mark transaction as failed
    /// </summary>
    /// <param name="reason">Reason for failure</param>
    public void Fail(string reason)
    {
        Status = TransactionStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = reason;
    }

    /// <summary>
    /// Mark transaction as processing
    /// </summary>
    public void MarkAsProcessing()
    {
        Status = TransactionStatus.Processing;
    }

    /// <summary>
    /// Flag transaction for review
    /// </summary>
    /// <param name="reason">Reason for flagging</param>
    public void Flag(string reason)
    {
        IsFlagged = true;
        FlaggedReason = reason;
        FlaggedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove flag from transaction
    /// </summary>
    public void Unflag()
    {
        IsFlagged = false;
        FlaggedReason = null;
        FlaggedAt = null;
    }

    /// <summary>
    /// Calculate hash for transaction integrity verification
    /// Uses HMAC-SHA256 with transaction data
    /// </summary>
    /// <param name="secretKey">Secret key for HMAC</param>
    /// <returns>Transaction hash</returns>
    public string CalculateHash(byte[] secretKey)
    {
        var data = $"{Id}:{FromUserId}:{ToUserId}:{Amount}:{Type}:{ProjectId}:{Description}:{CreatedAt:O}:{PreviousTransactionHash}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(secretKey);
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verify transaction hash integrity
    /// </summary>
    /// <param name="secretKey">Secret key for HMAC verification</param>
    /// <returns>True if hash is valid</returns>
    public bool VerifyHash(byte[] secretKey)
    {
        var calculatedHash = CalculateHash(secretKey);
        return string.Equals(TransactionHash, calculatedHash, StringComparison.Ordinal);
    }
}