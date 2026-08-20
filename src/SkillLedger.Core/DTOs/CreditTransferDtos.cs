using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs;

#region Request DTOs

/// <summary>
/// Request to initiate a single credit transfer
/// </summary>
public class TransferCreditsRequestDto
{
    /// <summary>
    /// User ID of the recipient
    /// </summary>
    [Required]
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Amount to transfer (must be positive)
    /// </summary>
    [Required]
    [Range(1, 10000, ErrorMessage = "Transfer amount must be between 1 and 10,000 credits")]
    public int Amount { get; set; }

    /// <summary>
    /// Optional message to recipient
    /// </summary>
    [MaxLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
    public string? Message { get; set; }

    /// <summary>
    /// BUG-040 FIX: Optional idempotency key to prevent duplicate processing
    /// Clients should generate a unique key (e.g., GUID) for each transfer request
    /// If provided, the same key cannot be used for multiple transfers
    /// </summary>
    [MaxLength(128)]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Single transfer within a batch
/// </summary>
public class BatchTransferItemDto
{
    /// <summary>
    /// Recipient user ID
    /// </summary>
    [Required]
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Amount to transfer to this recipient
    /// </summary>
    [Required]
    [Range(1, 10000, ErrorMessage = "Transfer amount must be between 1 and 10,000 credits")]
    public int Amount { get; set; }

    /// <summary>
    /// Optional message to this recipient
    /// </summary>
    [MaxLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
    public string? Message { get; set; }
}

/// <summary>
/// Request to initiate a batch credit transfer
/// </summary>
public class BatchTransferRequestDto
{
    /// <summary>
    /// List of transfers to execute
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one transfer is required")]
    [MaxLength(10, ErrorMessage = "Maximum 10 transfers allowed per batch")]
    public List<BatchTransferItemDto> Transfers { get; set; } = new();
}

/// <summary>
/// Request to reverse a completed transfer
/// </summary>
public class ReverseTransferRequestDto
{
    /// <summary>
    /// Reason for reversal
    /// </summary>
    [Required]
    [MaxLength(1000, ErrorMessage = "Reversal reason cannot exceed 1000 characters")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request for transfer history with pagination
/// </summary>
public class TransferHistoryRequestDto
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by transfer status
    /// </summary>
    public TransferStatus? Status { get; set; }

    /// <summary>
    /// Filter by transfer type (sent/received)
    /// </summary>
    public TransferDirection? Direction { get; set; }

    /// <summary>
    /// Start date for filtering
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Request for receipt verification
/// </summary>
public class VerifyReceiptRequestDto
{
    /// <summary>
    /// Transfer ID to verify
    /// </summary>
    [Required]
    public Guid TransferId { get; set; }

    /// <summary>
    /// Receipt signature to verify
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
}

#endregion

#region Response DTOs

/// <summary>
/// Response for a successful credit transfer
/// </summary>
public class TransferCreditsResponseDto
{
    /// <summary>
    /// Transfer ID for tracking
    /// </summary>
    public Guid TransferId { get; set; }

    /// <summary>
    /// Transaction hash for verification
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Current transfer status
    /// </summary>
    public TransferStatus Status { get; set; }

    /// <summary>
    /// Amount transferred
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Transfer fee charged
    /// </summary>
    public int TransferFee { get; set; }

    /// <summary>
    /// When the transfer was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Sender's remaining balance after transfer
    /// </summary>
    public int RemainingBalance { get; set; }
}

/// <summary>
/// Response for a batch transfer operation
/// </summary>
public class BatchTransferResponseDto
{
    /// <summary>
    /// Batch ID for tracking
    /// </summary>
    public Guid BatchId { get; set; }

    /// <summary>
    /// Individual transfer results
    /// </summary>
    public List<TransferCreditsResponseDto> Transfers { get; set; } = new();

    /// <summary>
    /// Total amount transferred
    /// </summary>
    public int TotalAmount { get; set; }

    /// <summary>
    /// Total fees charged
    /// </summary>
    public int TotalFees { get; set; }

    /// <summary>
    /// Number of successful transfers
    /// </summary>
    public int SuccessfulTransfers { get; set; }

    /// <summary>
    /// Number of failed transfers
    /// </summary>
    public int FailedTransfers { get; set; }

    /// <summary>
    /// Sender's remaining balance
    /// </summary>
    public int RemainingBalance { get; set; }

    /// <summary>
    /// BUG-021 FIX: Error details for failed transfers
    /// </summary>
    public List<BatchTransferError> Errors { get; set; } = new();
}

/// <summary>
/// BUG-021 FIX: Error information for failed batch transfer items
/// </summary>
public class BatchTransferError
{
    /// <summary>
    /// Recipient user ID that failed
    /// </summary>
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Amount that failed to transfer
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Detailed information about a credit transfer
/// </summary>
public class CreditTransferDetailDto
{
    /// <summary>
    /// Transfer ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Sender user ID
    /// </summary>
    public Guid FromUserId { get; set; }

    /// <summary>
    /// Sender username
    /// </summary>
    public string FromUsername { get; set; } = string.Empty;

    /// <summary>
    /// Recipient user ID
    /// </summary>
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Recipient username
    /// </summary>
    public string ToUsername { get; set; } = string.Empty;

    /// <summary>
    /// Transfer amount
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Transfer fee
    /// </summary>
    public int TransferFee { get; set; }

    /// <summary>
    /// Message from sender
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public TransferStatus Status { get; set; }

    /// <summary>
    /// Transaction hash
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Batch ID if part of batch
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// When created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When reversed (if applicable)
    /// </summary>
    public DateTime? ReversedAt { get; set; }

    /// <summary>
    /// Reversal reason (if applicable)
    /// </summary>
    public string? ReversalReason { get; set; }

    /// <summary>
    /// Whether transfer can be reversed
    /// </summary>
    public bool CanBeReversed { get; set; }
}

/// <summary>
/// Paginated transfer history response
/// </summary>
public class TransferHistoryResponseDto
{
    /// <summary>
    /// List of transfers
    /// </summary>
    public List<CreditTransferDetailDto> Transfers { get; set; } = new();

    /// <summary>
    /// Total number of transfers matching filter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there are previous pages
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Transfer limits and restrictions
/// </summary>
public class TransferLimitsDto
{
    /// <summary>
    /// Maximum amount per single transfer
    /// </summary>
    public int MaxSingleTransfer { get; set; }

    /// <summary>
    /// Maximum daily transfer amount
    /// </summary>
    public int MaxDailyTransfer { get; set; }

    /// <summary>
    /// Maximum number of transfers per day
    /// </summary>
    public int MaxDailyCount { get; set; }

    /// <summary>
    /// Amount already transferred today
    /// </summary>
    public int DailyTransferredAmount { get; set; }

    /// <summary>
    /// Number of transfers made today
    /// </summary>
    public int DailyTransferCount { get; set; }

    /// <summary>
    /// Remaining daily transfer amount
    /// </summary>
    public int RemainingDailyAmount { get; set; }

    /// <summary>
    /// Remaining daily transfer count
    /// </summary>
    public int RemainingDailyCount { get; set; }

    /// <summary>
    /// Current user's wallet balance
    /// </summary>
    public int WalletBalance { get; set; }

    /// <summary>
    /// Transfer fee percentage (if applicable)
    /// </summary>
    public decimal TransferFeePercentage { get; set; }

    /// <summary>
    /// Reversal window in hours
    /// </summary>
    public int ReversalWindowHours { get; set; }
}

/// <summary>
/// Digital receipt for a transfer
/// </summary>
public class TransferReceiptDto
{
    /// <summary>
    /// Transfer ID
    /// </summary>
    public Guid TransferId { get; set; }

    /// <summary>
    /// Transaction hash
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Sender information
    /// </summary>
    public string FromUser { get; set; } = string.Empty;

    /// <summary>
    /// Recipient information
    /// </summary>
    public string ToUser { get; set; } = string.Empty;

    /// <summary>
    /// Amount transferred
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Transfer fee
    /// </summary>
    public int TransferFee { get; set; }

    /// <summary>
    /// Message from sender
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// When transfer was completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Digital signature for verification
    /// </summary>
    public string ReceiptSignature { get; set; } = string.Empty;

    /// <summary>
    /// Receipt generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Response for receipt verification
/// </summary>
public class VerifyReceiptResponseDto
{
    /// <summary>
    /// Whether receipt is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Transfer details if valid
    /// </summary>
    public CreditTransferDetailDto? Transfer { get; set; }

    /// <summary>
    /// Verification message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

#endregion

#region Enums

/// <summary>
/// Direction of transfer for filtering
/// </summary>
public enum TransferDirection
{
    /// <summary>
    /// Transfers sent by the user
    /// </summary>
    Sent = 0,

    /// <summary>
    /// Transfers received by the user
    /// </summary>
    Received = 1,

    /// <summary>
    /// Both sent and received transfers
    /// </summary>
    Both = 2
}

#endregion