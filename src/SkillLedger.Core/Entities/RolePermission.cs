using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Junction entity for many-to-many relationship between Roles and Permissions
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Unique identifier for this role-permission assignment
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to Role
    /// </summary>
    [Required]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Foreign key to Permission
    /// </summary>
    [Required]
    public Guid PermissionId { get; set; }

    /// <summary>
    /// When this assignment was granted
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who granted this permission (optional)
    /// </summary>
    public Guid? GrantedByUserId { get; set; }

    /// <summary>
    /// Whether this assignment is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to Role
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Navigation property to Permission
    /// </summary>
    public virtual Permission Permission { get; set; } = null!;

    /// <summary>
    /// Navigation property to User who granted this permission (optional)
    /// </summary>
    public virtual User? GrantedByUser { get; set; }
}