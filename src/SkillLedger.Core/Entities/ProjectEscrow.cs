using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Project escrow account for holding credits until project completion
/// Implements secure multi-party escrow with milestone-based releases
/// </summary>
public class ProjectEscrow
{
    public ProjectEscrow()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = EscrowStatus.Active;
        ReleasedAmount = 0;
    }

    /// <summary>
    /// Unique identifier for the escrow account
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the project this escrow is for
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the related project
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Foreign key to the client (project owner) who deposited funds
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Navigation property to the client user
    /// </summary>
    public User Client { get; set; } = null!;

    /// <summary>
    /// Foreign key to the provider (service provider) who will receive funds
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Navigation property to the provider user
    /// </summary>
    public User Provider { get; set; } = null!;

    /// <summary>
    /// Total amount of credits in escrow
    /// Must match project's CreditBudget at time of creation
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Escrow amount must be positive")]
    public int TotalAmount { get; set; }

    /// <summary>
    /// Amount that has been released to the provider so far
    /// Sum of all milestone releases
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ReleasedAmount { get; set; } = 0;

    /// <summary>
    /// Remaining amount still held in escrow
    /// Calculated property: TotalAmount - ReleasedAmount
    /// </summary>
    public int RemainingAmount => TotalAmount - ReleasedAmount;

    /// <summary>
    /// Current status of the escrow account
    /// </summary>
    public EscrowStatus Status { get; set; } = EscrowStatus.Active;

    /// <summary>
    /// When the escrow account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the escrow was completed (all funds released or refunded)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the escrow was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the escrow was created
    /// For audit and security purposes
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? CreatedFromIP { get; set; }

    /// <summary>
    /// Optional notes or comments about the escrow
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Reason for dispute if status is Disputed
    /// </summary>
    [MaxLength(1000)]
    public string? DisputeReason { get; set; }

    /// <summary>
    /// When the dispute was raised
    /// </summary>
    public DateTime? DisputedAt { get; set; }

    /// <summary>
    /// Admin user who resolved the dispute (if any)
    /// </summary>
    public Guid? DisputeResolvedByUserId { get; set; }

    /// <summary>
    /// Navigation property to admin who resolved dispute
    /// </summary>
    public User? DisputeResolvedByUser { get; set; }

    /// <summary>
    /// When the dispute was resolved
    /// </summary>
    public DateTime? DisputeResolvedAt { get; set; }

    /// <summary>
    /// Resolution notes from admin
    /// </summary>
    [MaxLength(1000)]
    public string? DisputeResolutionNotes { get; set; }

    /// <summary>
    /// Whether this escrow requires multi-signature approval for releases
    /// Applied to high-value projects (>1000 credits)
    /// </summary>
    public bool RequiresMultiSignature { get; set; } = false;

    /// <summary>
    /// Collection of milestone releases for this escrow
    /// </summary>
    public virtual ICollection<EscrowMilestone> Milestones { get; set; } = new List<EscrowMilestone>();

    /// <summary>
    /// Collection of credit transactions related to this escrow
    /// </summary>
    public virtual ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();

    /// <summary>
    /// Collection of audit logs for this escrow
    /// </summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Helper methods and business logic

    /// <summary>
    /// Check if the escrow is fully released
    /// </summary>
    public bool IsFullyReleased => ReleasedAmount >= TotalAmount;

    /// <summary>
    /// Check if the escrow can be released (not disputed or frozen)
    /// </summary>
    public bool CanBeReleased => Status == EscrowStatus.Active || Status == EscrowStatus.PartiallyReleased;

    /// <summary>
    /// Check if the escrow is in a terminal state (completed, cancelled, etc.)
    /// </summary>
    public bool IsTerminal => Status == EscrowStatus.Completed || Status == EscrowStatus.Cancelled;

    /// <summary>
    /// Get the percentage of funds released
    /// </summary>
    public decimal ReleasedPercentage => TotalAmount > 0 ? (decimal)ReleasedAmount / TotalAmount * 100 : 0;

    /// <summary>
    /// Release credits to provider (partial or full)
    /// </summary>
    /// <param name="amount">Amount to release</param>
    /// <returns>True if release is valid</returns>
    public bool ReleaseAmount(int amount)
    {
        if (amount <= 0 || amount > RemainingAmount || !CanBeReleased)
            return false;

        ReleasedAmount += amount;
        UpdatedAt = DateTime.UtcNow;

        // Update status based on completion
        if (IsFullyReleased)
        {
            Status = EscrowStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
        else if (ReleasedAmount > 0)
        {
            Status = EscrowStatus.PartiallyReleased;
        }

        return true;
    }

    /// <summary>
    /// Mark escrow as disputed
    /// </summary>
    /// <param name="reason">Reason for dispute</param>
    public void RaiseDispute(string reason)
    {
        Status = EscrowStatus.Disputed;
        DisputeReason = reason;
        DisputedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolve dispute and restore active status
    /// </summary>
    /// <param name="adminUserId">Admin who resolved the dispute</param>
    /// <param name="resolutionNotes">Notes about the resolution</param>
    public void ResolveDispute(Guid adminUserId, string? resolutionNotes = null)
    {
        Status = ReleasedAmount > 0 ? EscrowStatus.PartiallyReleased : EscrowStatus.Active;
        DisputeResolvedByUserId = adminUserId;
        DisputeResolvedAt = DateTime.UtcNow;
        DisputeResolutionNotes = resolutionNotes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel escrow and mark for refund
    /// </summary>
    public void Cancel()
    {
        Status = EscrowStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Freeze escrow for security or policy violations
    /// </summary>
    public void Freeze()
    {
        Status = EscrowStatus.Frozen;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unfreeze escrow and restore previous status
    /// </summary>
    public void Unfreeze()
    {
        Status = ReleasedAmount > 0 ? EscrowStatus.PartiallyReleased : EscrowStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the timestamp
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}