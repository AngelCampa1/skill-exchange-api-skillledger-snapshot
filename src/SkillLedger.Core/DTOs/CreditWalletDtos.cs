using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// Fraud analysis report for credit wallet security
/// </summary>
public class FraudAnalysisReport
{
    /// <summary>
    /// Overall risk level assessment
    /// </summary>
    public bool IsHighRisk { get; set; }

    /// <summary>
    /// Risk score from 0-100 (higher is more risky)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// List of risk factors identified
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// Number of transactions analyzed
    /// </summary>
    public int TransactionsAnalyzed { get; set; }

    /// <summary>
    /// Date range of analysis
    /// </summary>
    public DateTime AnalysisStartDate { get; set; }
    public DateTime AnalysisEndDate { get; set; }

    /// <summary>
    /// Recommended actions
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    /// Whether additional verification is recommended
    /// </summary>
    public bool RequiresAdditionalVerification { get; set; }
}

/// <summary>
/// Balance reconciliation report for a single wallet
/// </summary>
public class BalanceReconciliationReport
{
    /// <summary>
    /// User ID for the wallet being reconciled
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Balance stored in the wallet record
    /// </summary>
    public int StoredBalance { get; set; }

    /// <summary>
    /// Balance calculated from transaction history
    /// </summary>
    public int CalculatedBalance { get; set; }

    /// <summary>
    /// Whether the balances match
    /// </summary>
    public bool IsBalanced => StoredBalance == CalculatedBalance;

    /// <summary>
    /// Difference between stored and calculated (stored - calculated)
    /// </summary>
    public int Discrepancy => StoredBalance - CalculatedBalance;

    /// <summary>
    /// Number of transactions included in calculation
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// When the reconciliation was performed
    /// </summary>
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Any issues found during reconciliation
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Whether automatic correction was applied
    /// </summary>
    public bool AutoCorrected { get; set; }
}

/// <summary>
/// System-wide reconciliation report
/// </summary>
public class SystemReconciliationReport
{
    /// <summary>
    /// Total number of wallets reconciled
    /// </summary>
    public int WalletsReconciled { get; set; }

    /// <summary>
    /// Number of wallets with discrepancies
    /// </summary>
    public int WalletsWithDiscrepancies { get; set; }

    /// <summary>
    /// Total discrepancy amount (sum of all individual discrepancies)
    /// </summary>
    public int TotalDiscrepancy { get; set; }

    /// <summary>
    /// Individual wallet reconciliation reports (only for wallets with issues)
    /// </summary>
    public List<BalanceReconciliationReport> DetailedReports { get; set; } = new();

    /// <summary>
    /// When the system reconciliation was performed
    /// </summary>
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// System health status
    /// </summary>
    public string HealthStatus => WalletsWithDiscrepancies == 0 ? "Healthy" : "Requires Attention";

    /// <summary>
    /// Summary statistics
    /// </summary>
    public SystemStatistics Statistics { get; set; } = new();
}

/// <summary>
/// System statistics for reconciliation
/// </summary>
public class SystemStatistics
{
    /// <summary>
    /// Total credits in circulation
    /// </summary>
    public long TotalCreditsInCirculation { get; set; }

    /// <summary>
    /// Total credits in escrow
    /// </summary>
    public long TotalCreditsInEscrow { get; set; }

    /// <summary>
    /// Total starting credits awarded
    /// </summary>
    public long TotalStartingCreditsAwarded { get; set; }

    /// <summary>
    /// Total transactions processed
    /// </summary>
    public long TotalTransactions { get; set; }

    /// <summary>
    /// Average wallet balance
    /// </summary>
    public decimal AverageWalletBalance { get; set; }
}

/// <summary>
/// Complete wallet export data for GDPR compliance
/// </summary>
public class WalletExportData
{
    /// <summary>
    /// Wallet summary information
    /// </summary>
    public WalletSummary WalletSummary { get; set; } = new();

    /// <summary>
    /// Complete transaction history
    /// </summary>
    public List<TransactionExportRecord> TransactionHistory { get; set; } = new();

    /// <summary>
    /// When the export was generated
    /// </summary>
    public DateTime ExportTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Export format version
    /// </summary>
    public string ExportVersion { get; set; } = "1.0";

    /// <summary>
    /// User ID for the export
    /// </summary>
    public Guid UserId { get; set; }
}

/// <summary>
/// Wallet summary for export
/// </summary>
public class WalletSummary
{
    public Guid WalletId { get; set; }
    public int CurrentBalance { get; set; }
    public int PendingBalance { get; set; }
    public int TotalEarned { get; set; }
    public int TotalSpent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTransactionAt { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
}

/// <summary>
/// Transaction record for export
/// </summary>
public class TransactionExportRecord
{
    public Guid TransactionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FromUser { get; set; }
    public string? ToUser { get; set; }
    public string? ProjectReference { get; set; }
    public bool WasIncoming { get; set; }
}

/// <summary>
/// Financial summary report for a date range
/// </summary>
public class FinancialSummaryReport
{
    public Guid UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Summary totals
    public int StartingBalance { get; set; }
    public int EndingBalance { get; set; }
    public int TotalCreditsReceived { get; set; }
    public int TotalCreditsSpent { get; set; }
    public int NetChange => TotalCreditsReceived - TotalCreditsSpent;

    // Transaction breakdowns
    public Dictionary<CreditTransactionType, int> ReceiptsByType { get; set; } = new();
    public Dictionary<CreditTransactionType, int> ExpensesByType { get; set; } = new();

    // Statistics
    public int TransactionCount { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public int LargestSingleReceipt { get; set; }
    public int LargestSingleExpense { get; set; }

    // Project-related
    public int ProjectsCompleted { get; set; }
    public int ProjectsWorkedOn { get; set; }
    public int TotalProjectEarnings { get; set; }
}

/// <summary>
/// Real-time wallet update notification
/// </summary>
public class WalletUpdateNotification
{
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public int NewBalance { get; set; }
    public int PreviousBalance { get; set; }
    public int BalanceChange => NewBalance - PreviousBalance;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdateReason { get; set; } = string.Empty;
    public Guid? RelatedTransactionId { get; set; }
    public string NotificationType { get; set; } = string.Empty; // "credit_received", "credit_spent", "escrow_created", etc.
}

/// <summary>
/// Request DTO for creating escrow
/// </summary>
public class CreateEscrowRequest
{
    public Guid ProjectId { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for transferring credits
/// </summary>
public class TransferCreditsRequest
{
    public Guid ToUserId { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public CreditTransactionType TransactionType { get; set; }
    public Guid? ProjectId { get; set; }
}

/// <summary>
/// Request DTO for adding credits to own wallet (demo/testing feature)
/// </summary>
public class AddCreditsRequest
{
    /// <summary>
    /// Amount of credits to add (must be positive, max 10000 for safety)
    /// </summary>
    [Required]
    [Range(1, 10000, ErrorMessage = "Amount must be between 1 and 10,000 credits")]
    public int Amount { get; set; }

    /// <summary>
    /// Description/reason for adding credits
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional package ID for tracking which package was purchased
    /// </summary>
    public string? PackageId { get; set; }
}

/// <summary>
/// Response DTO for wallet operations
/// </summary>
public class WalletOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? TransactionId { get; set; }
    public int? NewBalance { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// DTO for wallet dashboard display
/// </summary>
public class WalletDashboardData
{
    public WalletSummary Wallet { get; set; } = new();
    public List<TransactionExportRecord> RecentTransactions { get; set; } = new();
    public List<EscrowSummary> ActiveEscrows { get; set; } = new();
    public decimal MonthlyEarnings { get; set; }
    public decimal MonthlySpending { get; set; }
    public int ProjectsCompleted { get; set; }
}

/// <summary>
/// Escrow summary for dashboard
/// </summary>
public class EscrowSummary
{
    public Guid EscrowTransactionId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}