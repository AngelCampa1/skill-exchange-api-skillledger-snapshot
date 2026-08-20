using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

public class ProjectApplication
{
    public ProjectApplication()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the project application
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the project being applied for
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Reference to the service provider applying
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Cover letter with application pitch
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string CoverLetter { get; set; } = null!;

    /// <summary>
    /// Proposed timeline in days to completion
    /// </summary>
    [Range(1, 365)]
    public int? ProposedTimeline { get; set; }

    /// <summary>
    /// Automatic skill match score (0.00 to 1.00)
    /// </summary>
    [Range(0.00, 1.00)]
    public decimal? SkillMatchScore { get; set; }

    /// <summary>
    /// Application status
    /// </summary>
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    /// <summary>
    /// When the application was submitted
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the application was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the application was reviewed by the client
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Optional feedback from client on application
    /// </summary>
    [MaxLength(1000)]
    public string? ClientFeedback { get; set; }

    /// <summary>
    /// IP address from which the application was submitted
    /// </summary>
    [MaxLength(45)]
    public string? SubmittedFromIP { get; set; }

    /// <summary>
    /// Whether the provider is available to start immediately
    /// </summary>
    public bool IsAvailableImmediately { get; set; } = false;

    /// <summary>
    /// Proposed budget in credits (optional override)
    /// </summary>
    [Range(50, 5000)]
    public int? ProposedBudget { get; set; }

    /// <summary>
    /// Navigation property to the project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation property to the service provider
    /// </summary>
    public virtual User Provider { get; set; } = null!;

    /// <summary>
    /// Navigation property for portfolio attachments
    /// </summary>
    public virtual ICollection<ProjectApplicationAttachment> Attachments { get; set; } = new List<ProjectApplicationAttachment>();

    /// <summary>
    /// Helper property to check if application can be withdrawn
    /// </summary>
    public bool CanBeWithdrawn => Status == ApplicationStatus.Pending;

    /// <summary>
    /// Helper property to check if application is under review
    /// </summary>
    public bool IsUnderReview => Status == ApplicationStatus.UnderReview;

    /// <summary>
    /// Helper property to get days since application submitted
    /// </summary>
    public int DaysSinceSubmitted => (DateTime.UtcNow - CreatedAt).Days;
}