using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class Permission
{
    /// <summary>
    /// Unique identifier for the permission
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique permission name/key (e.g., "CREATE_PROJECT", "EDIT_USER")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Human-readable description of what this permission allows
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Category/module this permission belongs to (e.g., "User Management", "Projects")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Whether this permission is active/enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this permission was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this permission was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for role-permission relationships
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}