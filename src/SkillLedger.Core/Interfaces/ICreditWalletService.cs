using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for secure credit wallet operations with AES-256 encryption
/// Handles all financial transactions, fraud detection, and audit compliance
/// </summary>
public interface ICreditWalletService
{
    #region Wallet Management

    /// <summary>
    /// Create a new encrypted credit wallet for a user
    /// Awards starting credits (100) to new verified users
    /// </summary>
    /// <param name="userId">User ID to create wallet for</param>
    /// <returns>Newly created wallet with decrypted values</returns>
    Task<CreditWallet> CreateWalletAsync(Guid userId);

    /// <summary>
    /// Retrieve user's wallet with decrypted financial data
    /// </summary>
    /// <param name="userId">User ID to get wallet for</param>
    /// <returns>Wallet with decrypted values or null if not found</returns>
    Task<CreditWallet?> GetWalletAsync(Guid userId);

    /// <summary>
    /// Get wallet balance without full wallet details
    /// Optimized for frequent balance checks
    /// </summary>
    /// <param name="userId">User ID to check balance for</param>
    /// <returns>Current balance or null if wallet not found</returns>
    Task<int?> GetBalanceAsync(Guid userId);

    /// <summary>
    /// Get available balance (balance minus pending)
    /// </summary>
    /// <param name="userId">User ID to check available balance for</param>
    /// <returns>Available balance or null if wallet not found</returns>
    Task<int?> GetAvailableBalanceAsync(Guid userId);

    #endregion

    #region Transaction Operations

    /// <summary>
    /// Transfer credits between users with fraud detection and audit logging
    /// </summary>
    /// <param name="fromUserId">Sender user ID</param>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="amount">Amount to transfer (must be positive)</param>
    /// <param name="description">Transaction description</param>
    /// <param name="transactionType">Type of transaction</param>
    /// <param name="projectId">Optional project ID for project-related transactions</param>
    /// <param name="initiatedFromIP">IP address of client initiating transaction</param>
    /// <param name="userAgent">User agent of client</param>
    /// <returns>Created transaction record</returns>
    Task<CreditTransaction> TransferCreditsAsync(
        Guid fromUserId,
        Guid toUserId,
        int amount,
        string description,
        CreditTransactionType transactionType,
        Guid? projectId = null,
        string? initiatedFromIP = null,
        string? userAgent = null);

    /// <summary>
    /// Add credits to a wallet (system operations like rewards, adjustments)
    /// </summary>
    /// <param name="userId">User ID to add credits to</param>
    /// <param name="amount">Amount to add</param>
    /// <param name="description">Reason for adding credits</param>
    /// <param name="transactionType">Type of credit addition</param>
    /// <returns>Created transaction record</returns>
    Task<CreditTransaction> AddCreditsAsync(
        Guid userId,
        int amount,
        string description,
        CreditTransactionType transactionType);

    /// <summary>
    /// Deduct credits from a wallet (system operations like fees, penalties)
    /// </summary>
    /// <param name="userId">User ID to deduct credits from</param>
    /// <param name="amount">Amount to deduct</param>
    /// <param name="description">Reason for deducting credits</param>
    /// <param name="transactionType">Type of credit deduction</param>
    /// <returns>Created transaction record</returns>
    Task<CreditTransaction> DeductCreditsAsync(
        Guid userId,
        int amount,
        string description,
        CreditTransactionType transactionType);

    #endregion

    #region Escrow Operations

    /// <summary>
    /// Place credits in escrow for a project
    /// Credits are held until project completion or cancellation
    /// </summary>
    /// <param name="clientUserId">Client user ID (who pays)</param>
    /// <param name="projectId">Project ID for escrow</param>
    /// <param name="amount">Amount to place in escrow</param>
    /// <returns>Escrow transaction record</returns>
    Task<CreditTransaction> CreateEscrowAsync(Guid clientUserId, Guid projectId, int amount);

    /// <summary>
    /// Release escrowed credits to service provider upon project completion
    /// </summary>
    /// <param name="projectId">Project ID to release escrow for</param>
    /// <param name="providerUserId">Service provider to receive credits</param>
    /// <returns>Release transaction record</returns>
    Task<CreditTransaction> ReleaseEscrowAsync(Guid projectId, Guid providerUserId);

    /// <summary>
    /// Refund escrowed credits to client (project cancellation or dispute)
    /// Only refunds the remaining amount after any milestone releases
    /// </summary>
    /// <param name="projectId">Project ID to refund escrow for</param>
    /// <param name="remainingAmount">Amount to refund (should be escrow.RemainingAmount)</param>
    /// <returns>Refund transaction record</returns>
    Task<CreditTransaction> RefundEscrowAsync(Guid projectId, int remainingAmount);

    /// <summary>
    /// Release partial milestone payment from escrow to provider
    /// Reduces client's PendingBalance and increases provider's Balance
    /// </summary>
    /// <param name="clientUserId">Client user ID (whose escrow is being released)</param>
    /// <param name="providerUserId">Provider user ID (who receives the credits)</param>
    /// <param name="projectId">Project ID for the escrow</param>
    /// <param name="amount">Amount to release for this milestone</param>
    /// <returns>Release transaction record</returns>
    Task<CreditTransaction> ReleaseMilestoneFromEscrowAsync(
        Guid clientUserId,
        Guid providerUserId,
        Guid projectId,
        int amount);

    #endregion

    #region Transaction History & Audit

    /// <summary>
    /// Get complete transaction history for a user
    /// Returns immutable audit trail with cryptographic integrity verification
    /// </summary>
    /// <param name="userId">User ID to get history for</param>
    /// <param name="limit">Maximum number of transactions to return</param>
    /// <param name="offset">Number of transactions to skip</param>
    /// <returns>List of transactions ordered by creation date (newest first)</returns>
    Task<IList<CreditTransaction>> GetTransactionHistoryAsync(Guid userId, int limit = 50, int offset = 0);

    /// <summary>
    /// Get transactions for a specific project
    /// </summary>
    /// <param name="projectId">Project ID to get transactions for</param>
    /// <returns>List of project-related transactions</returns>
    Task<IList<CreditTransaction>> GetProjectTransactionsAsync(Guid projectId);

    /// <summary>
    /// Verify transaction integrity using cryptographic hash
    /// </summary>
    /// <param name="transactionId">Transaction ID to verify</param>
    /// <returns>True if transaction hash is valid</returns>
    Task<bool> ValidateTransactionIntegrity(Guid transactionId);

    #endregion

    #region Fraud Detection & Security

    /// <summary>
    /// Analyze user's transaction patterns for fraudulent activity
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <returns>Fraud risk assessment</returns>
    Task<FraudAnalysisReport> AnalyzeFraudPatterns(Guid userId);

    /// <summary>
    /// Block a wallet for security reasons
    /// </summary>
    /// <param name="userId">User ID to block</param>
    /// <param name="reason">Reason for blocking</param>
    /// <returns>True if successfully blocked</returns>
    Task<bool> BlockWalletAsync(Guid userId, string reason);

    /// <summary>
    /// Unblock a previously blocked wallet
    /// </summary>
    /// <param name="userId">User ID to unblock</param>
    /// <returns>True if successfully unblocked</returns>
    Task<bool> UnblockWalletAsync(Guid userId);

    #endregion

    #region Balance Reconciliation

    /// <summary>
    /// Reconcile wallet balance against transaction history
    /// Ensures balance accuracy and detects tampering
    /// </summary>
    /// <param name="userId">User ID to reconcile</param>
    /// <returns>Reconciliation report</returns>
    Task<BalanceReconciliationReport> ReconcileWalletBalance(Guid userId);

    /// <summary>
    /// Perform system-wide balance reconciliation
    /// Should be run periodically to ensure system integrity
    /// </summary>
    /// <returns>System reconciliation report</returns>
    Task<SystemReconciliationReport> ReconcileAllWallets();

    #endregion

    #region Encryption & Key Management

    /// <summary>
    /// Rotate encryption keys for all wallets
    /// Re-encrypts all wallet data with new keys from Azure Key Vault
    /// </summary>
    /// <returns>True if key rotation successful</returns>
    Task<bool> RotateEncryptionKeysAsync();

    /// <summary>
    /// Verify encryption integrity for a wallet
    /// </summary>
    /// <param name="userId">User ID to verify encryption for</param>
    /// <returns>True if encryption is intact</returns>
    Task<bool> VerifyEncryptionIntegrityAsync(Guid userId);

    #endregion

    #region Export & Reporting

    /// <summary>
    /// Export complete wallet data for user (GDPR compliance)
    /// </summary>
    /// <param name="userId">User ID to export data for</param>
    /// <returns>Complete wallet export data</returns>
    Task<WalletExportData> ExportWalletDataAsync(Guid userId);

    /// <summary>
    /// Generate financial summary report for a user
    /// </summary>
    /// <param name="userId">User ID to generate report for</param>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <returns>Financial summary report</returns>
    Task<FinancialSummaryReport> GenerateFinancialReportAsync(Guid userId, DateTime startDate, DateTime endDate);

    #endregion

    #region Real-time Updates

    /// <summary>
    /// Get real-time wallet updates (for SignalR notifications)
    /// </summary>
    /// <param name="userId">User ID to get updates for</param>
    /// <returns>Current wallet state for real-time updates</returns>
    Task<WalletUpdateNotification> GetWalletUpdateNotificationAsync(Guid userId);

    #endregion
}