using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a direct credit transfer between users
/// Provides advanced features like batch transfers, reversals, and receipt generation
/// </summary>
public class CreditTransfer
{
    /// <summary>
    /// Unique identifier for the transfer
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID of the sender
    /// </summary>
    [Required]
    public Guid FromUserId { get; set; }

    /// <summary>
    /// Navigation property to sender user
    /// </summary>
    public User FromUser { get; set; } = null!;

    /// <summary>
    /// User ID of the recipient
    /// </summary>
    [Required]
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Navigation property to recipient user
    /// </summary>
    public User ToUser { get; set; } = null!;

    /// <summary>
    /// Amount of credits being transferred (must be positive)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Transfer amount must be at least 1 credit")]
    public int Amount { get; set; }

    /// <summary>
    /// Transaction fee charged for this transfer (if applicable)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TransferFee { get; set; } = 0;

    /// <summary>
    /// Optional message from sender to recipient
    /// </summary>
    [MaxLength(500)]
    public string? Message { get; set; }

    /// <summary>
    /// Current status of the transfer
    /// </summary>
    [Required]
    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary>
    /// Unique cryptographic hash for transfer verification
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the underlying credit transaction
    /// </summary>
    public Guid? CreditTransactionId { get; set; }

    /// <summary>
    /// Navigation property to the credit transaction
    /// </summary>
    public CreditTransaction? CreditTransaction { get; set; }

    /// <summary>
    /// Batch ID for grouping multiple transfers together
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// BUG-040 FIX: Idempotency key to prevent duplicate transfer processing
    /// Clients should generate a unique key (e.g., GUID) for each transfer request
    /// If a transfer with the same idempotency key exists, return the existing transfer
    /// </summary>
    [MaxLength(128)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// IP address from which the transfer was initiated
    /// </summary>
    [MaxLength(45)]
    public string? InitiatedFromIP { get; set; }

    /// <summary>
    /// User agent of the client that initiated the transfer
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the transfer was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the transfer was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the transfer was reversed (if applicable)
    /// </summary>
    public DateTime? ReversedAt { get; set; }

    /// <summary>
    /// Reason for reversal (if applicable)
    /// </summary>
    [MaxLength(1000)]
    public string? ReversalReason { get; set; }

    /// <summary>
    /// User who performed the reversal (if applicable)
    /// </summary>
    public Guid? ReversedByUserId { get; set; }

    /// <summary>
    /// Navigation property to user who performed reversal
    /// </summary>
    public User? ReversedByUser { get; set; }

    /// <summary>
    /// Receipt signature for verification
    /// </summary>
    [MaxLength(512)]
    public string? ReceiptSignature { get; set; }

    /// <summary>
    /// Additional metadata for the transfer (JSON format)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Concurrency control
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    #region Business Logic Methods

    /// <summary>
    /// Check if this transfer can be reversed
    /// Transfers can only be reversed within 24 hours and if completed
    /// </summary>
    /// <returns>True if transfer can be reversed</returns>
    public bool CanBeReversed()
    {
        if (Status != TransferStatus.Completed || ReversedAt.HasValue)
            return false;

        if (!CompletedAt.HasValue)
            return false;

        // 24-hour reversal window
        return DateTime.UtcNow.Subtract(CompletedAt.Value).TotalHours <= 24;
    }

    /// <summary>
    /// Mark transfer as completed
    /// </summary>
    public void Complete()
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot complete transfer in {Status} status");

        Status = TransferStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark transfer as failed
    /// </summary>
    /// <param name="reason">Reason for failure</param>
    public void Fail(string reason)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot fail transfer in {Status} status");

        Status = TransferStatus.Failed;
        ReversalReason = reason;
    }

    /// <summary>
    /// Reverse a completed transfer
    /// </summary>
    /// <param name="reversedByUserId">User performing the reversal</param>
    /// <param name="reason">Reason for reversal</param>
    public void Reverse(Guid reversedByUserId, string reason)
    {
        if (!CanBeReversed())
            throw new InvalidOperationException("Transfer cannot be reversed at this time");

        Status = TransferStatus.Reversed;
        ReversedAt = DateTime.UtcNow;
        ReversedByUserId = reversedByUserId;
        ReversalReason = reason;
    }

    /// <summary>
    /// Generate a cryptographic hash for this transfer
    /// </summary>
    /// <returns>SHA-256 hash of transfer details</returns>
    public string GenerateTransactionHash()
    {
        var data = $"{FromUserId}|{ToUserId}|{Amount}|{CreatedAt:O}|{Id}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Generate a receipt signature for verification
    /// </summary>
    /// <param name="secretKey">Secret key for signing</param>
    /// <returns>HMAC signature</returns>
    public string GenerateReceiptSignature(string secretKey)
    {
        var data = $"{Id}|{TransactionHash}|{Amount}|{CompletedAt:O}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var signatureBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Verify receipt signature
    /// </summary>
    /// <param name="signature">Signature to verify</param>
    /// <param name="secretKey">Secret key for verification</param>
    /// <returns>True if signature is valid</returns>
    public bool VerifyReceiptSignature(string signature, string secretKey)
    {
        var expectedSignature = GenerateReceiptSignature(secretKey);
        return string.Equals(signature, expectedSignature, StringComparison.Ordinal);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Total amount including fees
    /// </summary>
    public int TotalAmount => Amount + TransferFee;

    /// <summary>
    /// Check if transfer is in a terminal state
    /// </summary>
    public bool IsTerminal => Status == TransferStatus.Completed ||
                             Status == TransferStatus.Failed ||
                             Status == TransferStatus.Reversed;

    /// <summary>
    /// Check if transfer is part of a batch
    /// </summary>
    public bool IsBatchTransfer => BatchId.HasValue;

    #endregion
}