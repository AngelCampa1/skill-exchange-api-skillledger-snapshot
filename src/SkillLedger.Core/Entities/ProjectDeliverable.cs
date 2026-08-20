using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class ProjectDeliverable
{
    public ProjectDeliverable()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the deliverable
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the project this deliverable belongs to
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Description of what needs to be delivered
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Order index for displaying deliverables in sequence
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Whether this deliverable is required for project completion
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Whether this deliverable has been completed
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// When this deliverable was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the deliverable was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the project this deliverable belongs to
    /// </summary>
    public virtual Project Project { get; set; } = null!;
}