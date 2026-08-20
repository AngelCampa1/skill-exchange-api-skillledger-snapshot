using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

public class Project
{
    public Project()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the project
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the client who posted the project
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Reference to the provider assigned to the project (nullable)
    /// </summary>
    public Guid? ProviderId { get; set; }

    /// <summary>
    /// Project title with business rule validation
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// Rich text description with XSS protection
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Current project status
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    /// <summary>
    /// Credit budget for the project (business rule: 50-5000 credits)
    /// </summary>
    [Range(50, 5000)]
    public int CreditBudget { get; set; }

    /// <summary>
    /// When the project work should start
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When the project should be completed
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Content moderation status
    /// </summary>
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;

    /// <summary>
    /// Optional moderation notes from review process
    /// </summary>
    [MaxLength(1000)]
    public string? ModerationNotes { get; set; }

    /// <summary>
    /// When the project was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the project was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the project was completed (nullable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the project was cancelled (nullable)
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Reason for dispute (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? DisputeReason { get; set; }

    /// <summary>
    /// IP address from which the project was created (for audit purposes)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? CreatedFromIP { get; set; }

    // Geolocation properties for search filtering

    /// <summary>
    /// Latitude coordinate for geolocation-based search
    /// </summary>
    public double? LocationLatitude { get; set; }

    /// <summary>
    /// Longitude coordinate for geolocation-based search
    /// </summary>
    public double? LocationLongitude { get; set; }

    /// <summary>
    /// City name for location-based filtering
    /// </summary>
    [MaxLength(100)]
    public string? LocationCity { get; set; }

    /// <summary>
    /// State/Province for location-based filtering
    /// </summary>
    [MaxLength(100)]
    public string? LocationState { get; set; }

    /// <summary>
    /// Country for location-based filtering
    /// </summary>
    [MaxLength(100)]
    public string? LocationCountry { get; set; }

    /// <summary>
    /// Whether this project supports remote work
    /// </summary>
    public bool IsRemoteWork { get; set; }


    /// <summary>
    /// Search-optimized concatenated text for full-text search
    /// Includes title, description, deliverables, and skill names
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Project complexity score (1-10 scale, calculated from duration, budget, skills)
    /// </summary>
    public int ComplexityScore { get; set; } = 5;

    /// <summary>
    /// Urgency indicator based on start date proximity
    /// </summary>
    public bool IsUrgent { get; set; } = false;

    /// <summary>
    /// Featured project boost for premium clients
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Project visibility settings
    /// </summary>
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Public;

    /// <summary>
    /// Navigation property to the client who posted the project
    /// </summary>
    public virtual User Client { get; set; } = null!;

    /// <summary>
    /// Navigation property to the provider assigned to the project
    /// </summary>
    public virtual User? Provider { get; set; }

    /// <summary>
    /// Navigation property for project deliverables
    /// </summary>
    public virtual ICollection<ProjectDeliverable> Deliverables { get; set; } = new List<ProjectDeliverable>();

    /// <summary>
    /// Navigation property for project skills requirements
    /// </summary>
    public virtual ICollection<ProjectSkill> ProjectSkills { get; set; } = new List<ProjectSkill>();

    /// <summary>
    /// Navigation property for audit logs related to this project
    /// </summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    /// <summary>
    /// Helper property to check if the project has valid timeline
    /// </summary>
    public bool HasValidTimeline => StartDate.HasValue && EndDate.HasValue && EndDate > StartDate && EndDate > DateTime.UtcNow;

    /// <summary>
    /// Helper property to check if the project is editable
    /// </summary>
    public bool IsEditable => Status == ProjectStatus.Draft;

    /// <summary>
    /// Helper property to check if the project can be published
    /// </summary>
    public bool CanBePublished => Status == ProjectStatus.Draft &&
                                 !string.IsNullOrWhiteSpace(Title) &&
                                 !string.IsNullOrWhiteSpace(Description) &&
                                 Deliverables.Any() &&
                                 ProjectSkills.Any() &&
                                 HasValidTimeline;
}