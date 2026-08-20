using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for credit transfer operations between users
/// Provides direct peer-to-peer transfers with advanced features like batch operations and reversals
/// </summary>
public interface ICreditTransferService
{
    #region Single Transfer Operations

    /// <summary>
    /// Transfer credits from one user to another
    /// </summary>
    /// <param name="fromUserId">Sender user ID</param>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="amount">Amount to transfer</param>
    /// <param name="message">Optional message to recipient</param>
    /// <param name="initiatedFromIP">IP address of sender</param>
    /// <param name="userAgent">User agent of sender</param>
    /// <param name="idempotencyKey">Optional idempotency key to prevent duplicate processing</param>
    /// <returns>Transfer response with details</returns>
    Task<TransferCreditsResponseDto> TransferCreditsAsync(
        Guid fromUserId,
        Guid toUserId,
        int amount,
        string? message = null,
        string? initiatedFromIP = null,
        string? userAgent = null,
        string? idempotencyKey = null);

    /// <summary>
    /// Get detailed information about a specific transfer
    /// </summary>
    /// <param name="transferId">Transfer ID to retrieve</param>
    /// <param name="userId">User ID requesting the information (for authorization)</param>
    /// <returns>Transfer details or null if not found/unauthorized</returns>
    Task<CreditTransferDetailDto?> GetTransferDetailsAsync(Guid transferId, Guid userId);

    #endregion

    #region Batch Transfer Operations

    /// <summary>
    /// Execute multiple transfers in a single batch operation
    /// </summary>
    /// <param name="fromUserId">Sender user ID</param>
    /// <param name="transfers">List of transfers to execute</param>
    /// <param name="initiatedFromIP">IP address of sender</param>
    /// <param name="userAgent">User agent of sender</param>
    /// <returns>Batch transfer response with individual results</returns>
    Task<BatchTransferResponseDto> BatchTransferAsync(
        Guid fromUserId,
        List<BatchTransferItemDto> transfers,
        string? initiatedFromIP = null,
        string? userAgent = null);

    #endregion

    #region Transfer History and Search

    /// <summary>
    /// Get paginated transfer history for a user
    /// </summary>
    /// <param name="userId">User ID to get history for</param>
    /// <param name="request">History request with filters and pagination</param>
    /// <returns>Paginated transfer history</returns>
    Task<TransferHistoryResponseDto> GetTransferHistoryAsync(
        Guid userId,
        TransferHistoryRequestDto request);

    /// <summary>
    /// Get transfers for a specific user (sent or received)
    /// </summary>
    /// <param name="userId">User ID to get transfers for</param>
    /// <param name="direction">Direction filter (sent/received/both)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated transfers</returns>
    Task<TransferHistoryResponseDto> GetUserTransfersAsync(
        Guid userId,
        TransferDirection direction = TransferDirection.Both,
        int page = 1,
        int pageSize = 20);

    #endregion

    #region Transfer Reversal

    /// <summary>
    /// Reverse a completed transfer within the reversal window
    /// </summary>
    /// <param name="transferId">Transfer ID to reverse</param>
    /// <param name="reversedByUserId">User performing the reversal</param>
    /// <param name="reason">Reason for reversal</param>
    /// <returns>True if reversal was successful</returns>
    Task<bool> ReverseTransferAsync(Guid transferId, Guid reversedByUserId, string reason);

    /// <summary>
    /// Check if a transfer can be reversed
    /// </summary>
    /// <param name="transferId">Transfer ID to check</param>
    /// <param name="userId">User ID requesting the check</param>
    /// <returns>True if transfer can be reversed</returns>
    Task<bool> CanReverseTransferAsync(Guid transferId, Guid userId);

    #endregion

    #region Transfer Limits and Validation

    /// <summary>
    /// Get current transfer limits for a user
    /// </summary>
    /// <param name="userId">User ID to get limits for</param>
    /// <returns>Transfer limits and current usage</returns>
    Task<TransferLimitsDto> GetTransferLimitsAsync(Guid userId);

    /// <summary>
    /// Validate if a transfer can be executed
    /// </summary>
    /// <param name="fromUserId">Sender user ID</param>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="amount">Amount to transfer</param>
    /// <returns>True if transfer can be executed</returns>
    Task<bool> ValidateTransferAsync(Guid fromUserId, Guid toUserId, int amount);

    /// <summary>
    /// Validate a batch transfer can be executed
    /// </summary>
    /// <param name="fromUserId">Sender user ID</param>
    /// <param name="transfers">List of transfers to validate</param>
    /// <returns>True if all transfers can be executed</returns>
    Task<bool> ValidateBatchTransferAsync(Guid fromUserId, List<BatchTransferItemDto> transfers);

    #endregion

    #region Receipt Generation and Verification

    /// <summary>
    /// Generate a digital receipt for a completed transfer
    /// </summary>
    /// <param name="transferId">Transfer ID to generate receipt for</param>
    /// <param name="userId">User ID requesting the receipt</param>
    /// <returns>Digital receipt with verification signature</returns>
    Task<TransferReceiptDto?> GenerateReceiptAsync(Guid transferId, Guid userId);

    /// <summary>
    /// Verify the authenticity of a transfer receipt
    /// </summary>
    /// <param name="transferId">Transfer ID to verify</param>
    /// <param name="signature">Receipt signature to verify</param>
    /// <returns>Verification result with transfer details</returns>
    Task<VerifyReceiptResponseDto> VerifyReceiptAsync(Guid transferId, string signature);

    #endregion

    #region Fraud Detection and Security

    /// <summary>
    /// Analyze transfer patterns for potential fraud
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="amount">Transfer amount to check</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Risk assessment result</returns>
    Task<FraudAssessmentResult> AnalyzeTransferRiskAsync(Guid userId, int amount, string? ipAddress = null);

    /// <summary>
    /// Get transfer statistics for fraud detection
    /// </summary>
    /// <param name="userId">User ID to get statistics for</param>
    /// <param name="timeframe">Time frame for statistics (default: 24 hours)</param>
    /// <returns>Transfer statistics</returns>
    Task<TransferStatistics> GetTransferStatisticsAsync(Guid userId, TimeSpan? timeframe = null);

    #endregion

    #region System Operations

    /// <summary>
    /// Get system-wide transfer statistics (admin only)
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>System transfer statistics</returns>
    Task<SystemTransferStatistics> GetSystemTransferStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Cancel a pending transfer (before processing)
    /// </summary>
    /// <param name="transferId">Transfer ID to cancel</param>
    /// <param name="cancelledByUserId">User performing the cancellation</param>
    /// <param name="reason">Reason for cancellation</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelTransferAsync(Guid transferId, Guid cancelledByUserId, string reason);

    #endregion
}

#region Supporting Types

/// <summary>
/// Result of fraud risk assessment
/// </summary>
public class FraudAssessmentResult
{
    /// <summary>
    /// Risk level (Low, Medium, High, Critical) - references RiskLevel from existing IEncryptionService
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Whether transfer should be allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Risk factors identified
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Recommended action
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// Transfer statistics for a user
/// </summary>
public class TransferStatistics
{
    /// <summary>
    /// Number of transfers sent
    /// </summary>
    public int TransfersSent { get; set; }

    /// <summary>
    /// Number of transfers received
    /// </summary>
    public int TransfersReceived { get; set; }

    /// <summary>
    /// Total amount sent
    /// </summary>
    public int TotalAmountSent { get; set; }

    /// <summary>
    /// Total amount received
    /// </summary>
    public int TotalAmountReceived { get; set; }

    /// <summary>
    /// Number of failed transfers
    /// </summary>
    public int FailedTransfers { get; set; }

    /// <summary>
    /// Number of reversed transfers
    /// </summary>
    public int ReversedTransfers { get; set; }

    /// <summary>
    /// Average transfer amount
    /// </summary>
    public decimal AverageTransferAmount { get; set; }

    /// <summary>
    /// Time period for these statistics
    /// </summary>
    public TimeSpan TimePeriod { get; set; }
}

/// <summary>
/// System-wide transfer statistics
/// </summary>
public class SystemTransferStatistics
{
    /// <summary>
    /// Total number of transfers
    /// </summary>
    public int TotalTransfers { get; set; }

    /// <summary>
    /// Total volume transferred
    /// </summary>
    public long TotalVolume { get; set; }

    /// <summary>
    /// Number of successful transfers
    /// </summary>
    public int SuccessfulTransfers { get; set; }

    /// <summary>
    /// Number of failed transfers
    /// </summary>
    public int FailedTransfers { get; set; }

    /// <summary>
    /// Number of reversed transfers
    /// </summary>
    public int ReversedTransfers { get; set; }

    /// <summary>
    /// Total fees collected
    /// </summary>
    public int TotalFees { get; set; }

    /// <summary>
    /// Average transfer amount
    /// </summary>
    public decimal AverageTransferAmount { get; set; }

    /// <summary>
    /// Peak transfer hour
    /// </summary>
    public int PeakTransferHour { get; set; }

    /// <summary>
    /// Number of unique users who made transfers
    /// </summary>
    public int ActiveTransferUsers { get; set; }

    /// <summary>
    /// Report generation date
    /// </summary>
    public DateTime ReportDate { get; set; }
}


#endregion