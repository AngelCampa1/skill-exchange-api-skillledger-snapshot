using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for project escrow operations with milestone-based releases
/// Provides secure escrow management for project-based credit transactions
/// </summary>
public interface IProjectEscrowService
{
    #region Escrow Management

    /// <summary>
    /// Create a new escrow account for a project
    /// Automatically deducts credits from client wallet and secures them in escrow
    /// </summary>
    /// <param name="projectId">Project to create escrow for</param>
    /// <param name="providerId">Service provider who will receive the funds</param>
    /// <param name="initiatedFromIP">IP address of client creating escrow</param>
    /// <returns>Created project escrow</returns>
    Task<ProjectEscrow> CreateEscrowAsync(Guid projectId, Guid providerId, string? initiatedFromIP = null);

    /// <summary>
    /// Get escrow account for a specific project
    /// </summary>
    /// <param name="projectId">Project ID to get escrow for</param>
    /// <returns>Project escrow or null if not found</returns>
    Task<ProjectEscrow?> GetEscrowByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Get escrow account by escrow ID
    /// </summary>
    /// <param name="escrowId">Escrow ID to retrieve</param>
    /// <returns>Project escrow or null if not found</returns>
    Task<ProjectEscrow?> GetEscrowByIdAsync(Guid escrowId);

    /// <summary>
    /// Get all active escrows for a user (client or provider)
    /// </summary>
    /// <param name="userId">User ID to get escrows for</param>
    /// <returns>List of active escrows</returns>
    Task<IList<ProjectEscrow>> GetActiveEscrowsForUserAsync(Guid userId);

    /// <summary>
    /// Get escrow history and audit trail
    /// </summary>
    /// <param name="escrowId">Escrow ID to get history for</param>
    /// <returns>Complete audit trail of escrow operations</returns>
    Task<IList<AuditLog>> GetEscrowHistoryAsync(Guid escrowId);

    #endregion

    #region Milestone Management

    /// <summary>
    /// Add a milestone to an escrow account
    /// Enables phased release of escrowed credits
    /// </summary>
    /// <param name="escrowId">Escrow to add milestone to</param>
    /// <param name="description">Milestone description</param>
    /// <param name="amount">Credits to release for this milestone</param>
    /// <param name="expectedCompletionDate">Expected completion date</param>
    /// <param name="linkedDeliverableId">Optional linked project deliverable</param>
    /// <param name="sequenceOrder">Order sequence for milestone</param>
    /// <returns>Created milestone</returns>
    Task<EscrowMilestone> AddMilestoneAsync(
        Guid escrowId,
        string description,
        int amount,
        DateTime? expectedCompletionDate = null,
        Guid? linkedDeliverableId = null,
        int? sequenceOrder = null);

    /// <summary>
    /// Release a specific milestone to the provider
    /// Transfers credits from escrow to provider wallet
    /// </summary>
    /// <param name="milestoneId">Milestone to release</param>
    /// <param name="approvedByUserId">User approving the release (typically client)</param>
    /// <param name="releaseNotes">Notes about the release</param>
    /// <returns>True if release successful</returns>
    Task<bool> ReleaseMilestoneAsync(Guid milestoneId, Guid approvedByUserId, string? releaseNotes = null);

    /// <summary>
    /// Get all milestones for an escrow
    /// </summary>
    /// <param name="escrowId">Escrow ID to get milestones for</param>
    /// <returns>List of milestones ordered by sequence</returns>
    Task<IList<EscrowMilestone>> GetMilestonesAsync(Guid escrowId);

    /// <summary>
    /// Update milestone expected completion date
    /// </summary>
    /// <param name="milestoneId">Milestone to update</param>
    /// <param name="newExpectedDate">New expected completion date</param>
    /// <returns>True if update successful</returns>
    Task<bool> UpdateMilestoneExpectedDateAsync(Guid milestoneId, DateTime newExpectedDate);

    #endregion

    #region Full Escrow Operations

    /// <summary>
    /// Release the entire escrow amount to the provider
    /// Used when project is completed without milestone tracking
    /// </summary>
    /// <param name="escrowId">Escrow to release fully</param>
    /// <param name="approvedByUserId">User approving the release (typically client)</param>
    /// <param name="releaseNotes">Notes about the full release</param>
    /// <returns>True if release successful</returns>
    Task<bool> ReleaseFullEscrowAsync(Guid escrowId, Guid approvedByUserId, string? releaseNotes = null);

    /// <summary>
    /// Cancel escrow and refund remaining credits to client
    /// Used when project is cancelled before completion
    /// </summary>
    /// <param name="escrowId">Escrow to cancel</param>
    /// <param name="cancelledByUserId">User cancelling the escrow</param>
    /// <param name="cancellationReason">Reason for cancellation</param>
    /// <returns>True if cancellation successful</returns>
    Task<bool> CancelEscrowAsync(Guid escrowId, Guid cancelledByUserId, string? cancellationReason = null);

    #endregion

    #region Dispute Management

    /// <summary>
    /// Raise a dispute on an escrow account
    /// Freezes further releases pending admin resolution
    /// </summary>
    /// <param name="escrowId">Escrow to dispute</param>
    /// <param name="raisedByUserId">User raising the dispute</param>
    /// <param name="disputeReason">Reason for the dispute</param>
    /// <returns>True if dispute raised successfully</returns>
    Task<bool> RaiseDisputeAsync(Guid escrowId, Guid raisedByUserId, string disputeReason);

    /// <summary>
    /// Resolve a dispute on an escrow account
    /// Only admin users can resolve disputes
    /// </summary>
    /// <param name="escrowId">Escrow with dispute to resolve</param>
    /// <param name="resolvedByUserId">Admin user resolving the dispute</param>
    /// <param name="resolutionAction">Action taken to resolve dispute</param>
    /// <param name="resolutionNotes">Notes about the resolution</param>
    /// <returns>True if dispute resolved successfully</returns>
    Task<bool> ResolveDisputeAsync(
        Guid escrowId,
        Guid resolvedByUserId,
        string resolutionAction,
        string? resolutionNotes = null);

    /// <summary>
    /// Get all disputed escrows for admin review
    /// </summary>
    /// <returns>List of escrows in disputed status</returns>
    Task<IList<ProjectEscrow>> GetDisputedEscrowsAsync();

    #endregion

    #region Security and Compliance

    /// <summary>
    /// Freeze an escrow account for security or compliance reasons
    /// Prevents any releases until unfrozen by admin
    /// </summary>
    /// <param name="escrowId">Escrow to freeze</param>
    /// <param name="frozenByUserId">Admin user freezing the escrow</param>
    /// <param name="freezeReason">Reason for freezing</param>
    /// <returns>True if freeze successful</returns>
    Task<bool> FreezeEscrowAsync(Guid escrowId, Guid frozenByUserId, string freezeReason);

    /// <summary>
    /// Unfreeze a previously frozen escrow account
    /// </summary>
    /// <param name="escrowId">Escrow to unfreeze</param>
    /// <param name="unfrozenByUserId">Admin user unfreezing the escrow</param>
    /// <returns>True if unfreeze successful</returns>
    Task<bool> UnfreezeEscrowAsync(Guid escrowId, Guid unfrozenByUserId);

    /// <summary>
    /// Validate escrow integrity and detect tampering
    /// Checks escrow data consistency and transaction history
    /// </summary>
    /// <param name="escrowId">Escrow to validate</param>
    /// <returns>True if escrow integrity is valid</returns>
    Task<bool> ValidateEscrowIntegrityAsync(Guid escrowId);

    #endregion

    #region Reporting and Analytics

    /// <summary>
    /// Get escrow statistics for a user
    /// </summary>
    /// <param name="userId">User to get statistics for</param>
    /// <returns>Escrow statistics summary</returns>
    Task<EscrowStatistics> GetEscrowStatisticsAsync(Guid userId);

    /// <summary>
    /// Get system-wide escrow metrics for admin dashboard
    /// </summary>
    /// <returns>System escrow metrics</returns>
    Task<SystemEscrowMetrics> GetSystemEscrowMetricsAsync();

    /// <summary>
    /// Generate escrow report for compliance and auditing
    /// </summary>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <returns>Comprehensive escrow report</returns>
    Task<EscrowComplianceReport> GenerateEscrowReportAsync(DateTime startDate, DateTime endDate);

    #endregion

    #region Real-time Updates

    /// <summary>
    /// Get real-time escrow updates for SignalR notifications
    /// </summary>
    /// <param name="userId">User to get updates for</param>
    /// <returns>Current escrow state for notifications</returns>
    Task<EscrowUpdateNotification> GetEscrowUpdateNotificationAsync(Guid userId);

    #endregion
}

#region DTOs and Supporting Types

/// <summary>
/// Escrow statistics for a user
/// </summary>
public class EscrowStatistics
{
    public int TotalEscrowsCreated { get; set; }
    public int ActiveEscrows { get; set; }
    public int CompletedEscrows { get; set; }
    public int DisputedEscrows { get; set; }
    public int TotalCreditsInEscrow { get; set; }
    public int TotalCreditsReleased { get; set; }
    public decimal AverageEscrowAmount { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime? LastEscrowActivity { get; set; }
}

/// <summary>
/// System-wide escrow metrics
/// </summary>
public class SystemEscrowMetrics
{
    public int TotalActiveEscrows { get; set; }
    public int TotalCreditsInEscrow { get; set; }
    public int EscrowsCreatedToday { get; set; }
    public int EscrowsCompletedToday { get; set; }
    public int PendingDisputes { get; set; }
    public int FrozenEscrows { get; set; }
    public decimal AverageReleaseTime { get; set; }
    public decimal DisputeRate { get; set; }
}

/// <summary>
/// Escrow compliance report
/// </summary>
public class EscrowComplianceReport
{
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public int TotalEscrows { get; set; }
    public int TotalCreditsProcessed { get; set; }
    public IList<ProjectEscrow> HighValueEscrows { get; set; } = new List<ProjectEscrow>();
    public IList<ProjectEscrow> DisputedEscrows { get; set; } = new List<ProjectEscrow>();
    public IList<AuditLog> SecurityEvents { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Real-time escrow update notification
/// </summary>
public class EscrowUpdateNotification
{
    public Guid UserId { get; set; }
    public int ActiveEscrowCount { get; set; }
    public int PendingReleases { get; set; }
    public int TotalCreditsInEscrow { get; set; }
    public IList<EscrowMilestone> UpcomingMilestones { get; set; } = new List<EscrowMilestone>();
    public IList<ProjectEscrow> RecentActivity { get; set; } = new List<ProjectEscrow>();
    public DateTime LastUpdated { get; set; }
}

#endregion