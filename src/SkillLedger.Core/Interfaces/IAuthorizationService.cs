using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for handling role-based authorization and permissions
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if a user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionName);

    /// <summary>
    /// Check if a user has any of the specified permissions
    /// </summary>
    Task<bool> HasAnyPermissionAsync(Guid userId, params string[] permissionNames);

    /// <summary>
    /// Check if a user has all of the specified permissions
    /// </summary>
    Task<bool> HasAllPermissionsAsync(Guid userId, params string[] permissionNames);

    /// <summary>
    /// Get all permissions for a user (through their roles)
    /// </summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Get all active roles for a user
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid? assignedByUserId = null);

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    Task<bool> RemoveRoleAsync(Guid userId, string roleName, Guid? removedByUserId = null);

    /// <summary>
    /// Get all permissions for a specific role
    /// </summary>
    Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Assign a permission to a role
    /// </summary>
    Task<bool> AssignPermissionToRoleAsync(string roleName, string permissionName, Guid? assignedByUserId = null);

    /// <summary>
    /// Remove a permission from a role
    /// </summary>
    Task<bool> RemovePermissionFromRoleAsync(string roleName, string permissionName, Guid? removedByUserId = null);

    /// <summary>
    /// Create a new role
    /// </summary>
    Task<Role?> CreateRoleAsync(CreateRoleDto roleDto, Guid? createdByUserId = null);

    /// <summary>
    /// Update an existing role
    /// </summary>
    Task<Role?> UpdateRoleAsync(Guid roleId, UpdateRoleDto roleDto, Guid? updatedByUserId = null);

    /// <summary>
    /// Delete a role (if not a system role and not assigned to any users)
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid roleId, Guid? deletedByUserId = null);

    /// <summary>
    /// Get all available permissions grouped by category
    /// </summary>
    Task<Dictionary<string, IEnumerable<PermissionDto>>> GetAllPermissionsByCategoryAsync();

    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesWithPermissionsAsync();

    /// <summary>
    /// Initialize system roles and permissions
    /// </summary>
    Task InitializeSystemRolesAndPermissionsAsync();
}