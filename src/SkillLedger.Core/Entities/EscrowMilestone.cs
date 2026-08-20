using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Milestone-based payment release for project escrow
/// Enables phased releases of escrowed credits based on deliverable completion
/// </summary>
public class EscrowMilestone
{
    public EscrowMilestone()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsReleased = false;
    }

    /// <summary>
    /// Unique identifier for the milestone
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the project escrow account
    /// </summary>
    public Guid EscrowId { get; set; }

    /// <summary>
    /// Navigation property to the escrow account
    /// </summary>
    public ProjectEscrow Escrow { get; set; } = null!;

    /// <summary>
    /// Human-readable description of the milestone
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Amount of credits to release when this milestone is completed
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Milestone amount must be positive")]
    public int Amount { get; set; }

    /// <summary>
    /// Percentage of total escrow amount this milestone represents
    /// Calculated property for display purposes
    /// </summary>
    public decimal Percentage => Escrow?.TotalAmount > 0 ? (decimal)Amount / Escrow.TotalAmount * 100 : 0;

    /// <summary>
    /// Whether this milestone has been released
    /// </summary>
    public bool IsReleased { get; set; } = false;

    /// <summary>
    /// When this milestone was released (null if not yet released)
    /// </summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>
    /// User who approved the release of this milestone
    /// Typically the client approving work completion
    /// </summary>
    public Guid? ReleasedByUserId { get; set; }

    /// <summary>
    /// Navigation property to user who approved release
    /// </summary>
    public User? ReleasedByUser { get; set; }

    /// <summary>
    /// Optional notes about the milestone release
    /// </summary>
    [MaxLength(1000)]
    public string? ReleaseNotes { get; set; }

    /// <summary>
    /// Expected completion date for this milestone (if applicable)
    /// Used for project timeline tracking
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Actual completion date when milestone was marked complete
    /// </summary>
    public DateTime? ActualCompletionDate { get; set; }

    /// <summary>
    /// Order sequence for this milestone within the project
    /// Used for displaying milestones in correct order
    /// </summary>
    public int SequenceOrder { get; set; } = 1;

    /// <summary>
    /// Whether this milestone is required before subsequent milestones can be released
    /// Enforces sequential completion when true
    /// </summary>
    public bool IsBlocking { get; set; } = false;

    /// <summary>
    /// When the milestone was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the milestone was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the milestone was created
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? CreatedFromIP { get; set; }

    /// <summary>
    /// Optional reference to project deliverable this milestone is tied to
    /// Links milestone releases to specific deliverable completion
    /// </summary>
    public Guid? LinkedDeliverableId { get; set; }

    /// <summary>
    /// Navigation property to linked project deliverable
    /// </summary>
    public ProjectDeliverable? LinkedDeliverable { get; set; }

    /// <summary>
    /// Collection of audit logs for this milestone
    /// </summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Helper methods and business logic

    /// <summary>
    /// Check if this milestone is overdue based on expected completion date
    /// </summary>
    public bool IsOverdue => ExpectedCompletionDate.HasValue &&
                           !IsReleased &&
                           DateTime.UtcNow > ExpectedCompletionDate.Value;

    /// <summary>
    /// Check if this milestone can be released
    /// </summary>
    public bool CanBeReleased => !IsReleased &&
                                Escrow?.CanBeReleased == true &&
                                Amount <= Escrow.RemainingAmount;

    /// <summary>
    /// Get days until expected completion (negative if overdue)
    /// </summary>
    public int? DaysUntilDue => ExpectedCompletionDate?.Subtract(DateTime.UtcNow).Days;

    /// <summary>
    /// Release this milestone
    /// </summary>
    /// <param name="releasedByUserId">User approving the release</param>
    /// <param name="releaseNotes">Optional notes about the release</param>
    /// <returns>True if release is successful</returns>
    public bool Release(Guid releasedByUserId, string? releaseNotes = null)
    {
        if (!CanBeReleased)
            return false;

        IsReleased = true;
        ReleasedAt = DateTime.UtcNow;
        ActualCompletionDate = DateTime.UtcNow;
        ReleasedByUserId = releasedByUserId;
        ReleaseNotes = releaseNotes;
        UpdatedAt = DateTime.UtcNow;

        return true;
    }

    /// <summary>
    /// Mark milestone as completed (ready for release approval)
    /// </summary>
    public void MarkCompleted()
    {
        ActualCompletionDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update expected completion date
    /// </summary>
    /// <param name="newDate">New expected completion date</param>
    public void UpdateExpectedCompletion(DateTime newDate)
    {
        ExpectedCompletionDate = newDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate completion performance (early/on-time/late)
    /// </summary>
    /// <returns>Performance indicator</returns>
    public string GetCompletionPerformance()
    {
        if (!ActualCompletionDate.HasValue || !ExpectedCompletionDate.HasValue)
            return "Unknown";

        var daysDifference = (ActualCompletionDate.Value - ExpectedCompletionDate.Value).Days;

        return daysDifference switch
        {
            < -1 => "Early",
            <= 1 => "On Time",
            _ => "Late"
        };
    }

    /// <summary>
    /// Update the timestamp
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}