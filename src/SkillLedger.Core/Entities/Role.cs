using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class Role : IdentityRole<Guid>
{
    public Role() : base()
    {
        Id = Guid.NewGuid();
    }

    public Role(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Human-readable description of the role
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this role is a system role (cannot be deleted/modified)
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Whether this role is active/enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority level for role hierarchy (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// When this role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this role was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for role-permission relationships
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}