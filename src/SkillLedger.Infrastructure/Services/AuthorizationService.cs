using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Constants;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

public class AuthorizationService : IAuthorizationService
{
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleNames.Admin
    };

    private static readonly HashSet<string> PrivilegedPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        PermissionNames.ManageRoles,
        PermissionNames.ManagePermissions,
        PermissionNames.ManageUserRoles,
        PermissionNames.ManageSystemSettings,
        PermissionNames.ManageCredits,
        PermissionNames.ADMIN_ESCROW_MANAGEMENT,
        PermissionNames.ADMIN_SYSTEM_METRICS
    };

    private readonly SkillLedgerDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        SkillLedgerDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IAuditLogService auditLogService,
        ILogger<AuthorizationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any()) return false;

            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .AnyAsync(rp =>
                    userRoles.Contains(rp.Role.Name!) &&
                    rp.Permission.Name == permissionName &&
                    rp.IsActive &&
                    rp.Permission.IsActive &&
                    rp.Role.IsActive);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permissionName, userId);
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(Guid userId, params string[] permissionNames)
    {
        if (!permissionNames.Any()) return false;

        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any()) return false;

            var hasAnyPermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .AnyAsync(rp =>
                    userRoles.Contains(rp.Role.Name!) &&
                    permissionNames.Contains(rp.Permission.Name) &&
                    rp.IsActive &&
                    rp.Permission.IsActive &&
                    rp.Role.IsActive);

            return hasAnyPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking any permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasAllPermissionsAsync(Guid userId, params string[] permissionNames)
    {
        if (!permissionNames.Any()) return true;

        try
        {
            foreach (var permission in permissionNames)
            {
                if (!await HasPermissionAsync(userId, permission))
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking all permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Array.Empty<string>();

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any()) return Array.Empty<string>();

            var permissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp =>
                    userRoles.Contains(rp.Role.Name!) &&
                    rp.IsActive &&
                    rp.Permission.IsActive &&
                    rp.Role.IsActive)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Array.Empty<string>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid? assignedByUserId = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role assignment", userId);
                return false;
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found for assignment", roleName);
                return false;
            }

            if (IsPrivilegedRole(roleName) && !await IsAdminUserAsync(assignedByUserId))
            {
                _logger.LogWarning(
                    "User {AssignedByUserId} attempted to assign privileged role {RoleName}",
                    assignedByUserId, roleName);
                return false;
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                _logger.LogDebug("User {UserId} already has role {RoleName}", userId, roleName);
                return true;
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                await _auditLogService.LogEventAsync(
                    assignedByUserId ?? userId,
                    "ROLE_ASSIGNED",
                    "system",
                    null,
                    true,
                    $"Role '{roleName}' assigned to user {userId}"
                );

                _logger.LogInformation("Role {RoleName} assigned to user {UserId} by {AssignedByUserId}",
                    roleName, userId, assignedByUserId);
                return true;
            }

            _logger.LogWarning("Failed to assign role {RoleName} to user {UserId}: {Errors}",
                roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, userId);
            return false;
        }
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName, Guid? removedByUserId = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role removal", userId);
                return false;
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                _logger.LogDebug("User {UserId} does not have role {RoleName}", userId, roleName);
                return true;
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                await _auditLogService.LogEventAsync(
                    removedByUserId ?? userId,
                    "ROLE_REMOVED",
                    "system",
                    null,
                    true,
                    $"Role '{roleName}' removed from user {userId}"
                );

                _logger.LogInformation("Role {RoleName} removed from user {UserId} by {RemovedByUserId}",
                    roleName, userId, removedByUserId);
                return true;
            }

            _logger.LogWarning("Failed to remove role {RoleName} from user {UserId}: {Errors}",
                roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName)
    {
        try
        {
            var permissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp =>
                    rp.Role.Name == roleName &&
                    rp.IsActive &&
                    rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name)
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for role {RoleName}", roleName);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> AssignPermissionToRoleAsync(string roleName, string permissionName, Guid? assignedByUserId = null)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found for permission assignment", roleName);
                return false;
            }

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permissionName && p.IsActive);
            if (permission == null)
            {
                _logger.LogWarning("Permission {PermissionName} not found for assignment", permissionName);
                return false;
            }

            if (IsPrivilegedPermission(permission.Name) && !await IsAdminUserAsync(assignedByUserId))
            {
                _logger.LogWarning(
                    "User {AssignedByUserId} attempted to assign privileged permission {PermissionName} to role {RoleName}",
                    assignedByUserId, permission.Name, roleName);
                return false;
            }

            // Check if already assigned
            var existingAssignment = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

            if (existingAssignment != null)
            {
                if (existingAssignment.IsActive)
                {
                    _logger.LogDebug("Permission {PermissionName} already assigned to role {RoleName}",
                        permissionName, roleName);
                    return true;
                }
                else
                {
                    // Reactivate existing assignment
                    existingAssignment.IsActive = true;
                    existingAssignment.GrantedAt = DateTime.UtcNow;
                    existingAssignment.GrantedByUserId = assignedByUserId;
                }
            }
            else
            {
                // Create new assignment
                var rolePermission = new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    GrantedByUserId = assignedByUserId
                };
                _context.RolePermissions.Add(rolePermission);
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                assignedByUserId,
                "PERMISSION_ASSIGNED_TO_ROLE",
                "system",
                null,
                true,
                $"Permission '{permissionName}' assigned to role '{roleName}'"
            );

            _logger.LogInformation("Permission {PermissionName} assigned to role {RoleName} by {AssignedByUserId}",
                permissionName, roleName, assignedByUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionName} to role {RoleName}",
                permissionName, roleName);
            return false;
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(string roleName, string permissionName, Guid? removedByUserId = null)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleName} not found for permission removal", roleName);
                return false;
            }

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permissionName);
            if (permission == null)
            {
                _logger.LogWarning("Permission {PermissionName} not found for removal", permissionName);
                return false;
            }

            var assignment = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

            if (assignment == null || !assignment.IsActive)
            {
                _logger.LogDebug("Permission {PermissionName} not assigned to role {RoleName}",
                    permissionName, roleName);
                return true;
            }

            assignment.IsActive = false;
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                removedByUserId,
                "PERMISSION_REMOVED_FROM_ROLE",
                "system",
                null,
                true,
                $"Permission '{permissionName}' removed from role '{roleName}'"
            );

            _logger.LogInformation("Permission {PermissionName} removed from role {RoleName} by {RemovedByUserId}",
                permissionName, roleName, removedByUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionName} from role {RoleName}",
                permissionName, roleName);
            return false;
        }
    }

    public async Task<Role?> CreateRoleAsync(CreateRoleDto roleDto, Guid? createdByUserId = null)
    {
        try
        {
            var existingRole = await _roleManager.FindByNameAsync(roleDto.Name);
            if (existingRole != null)
            {
                _logger.LogWarning("Role {RoleName} already exists", roleDto.Name);
                return null;
            }

            if (await ContainsPrivilegedPermissionsAsync(roleDto.PermissionIds) &&
                !await IsAdminUserAsync(createdByUserId))
            {
                _logger.LogWarning(
                    "User {CreatedByUserId} attempted to create role {RoleName} with privileged permissions",
                    createdByUserId, roleDto.Name);
                return null;
            }

            var role = new Role(roleDto.Name)
            {
                Description = roleDto.Description,
                Priority = roleDto.Priority,
                IsSystemRole = false,
                IsActive = true
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to create role {RoleName}: {Errors}",
                    roleDto.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                return null;
            }

            // Assign permissions if provided
            foreach (var permissionId in roleDto.PermissionIds)
            {
                var permission = await _context.Permissions.FindAsync(permissionId);
                if (permission?.IsActive == true)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        GrantedByUserId = createdByUserId
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                createdByUserId,
                "ROLE_CREATED",
                "system",
                null,
                true,
                $"Role '{roleDto.Name}' created"
            );

            _logger.LogInformation("Role {RoleName} created by {CreatedByUserId}", roleDto.Name, createdByUserId);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", roleDto.Name);
            return null;
        }
    }

    public async Task<Role?> UpdateRoleAsync(Guid roleId, UpdateRoleDto roleDto, Guid? updatedByUserId = null)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found for update", roleId);
                return null;
            }

            if (role.IsSystemRole)
            {
                _logger.LogWarning("Cannot update system role {RoleName}", role.Name);
                return null;
            }

            if (await ContainsPrivilegedPermissionsAsync(roleDto.PermissionIds) &&
                !await IsAdminUserAsync(updatedByUserId))
            {
                _logger.LogWarning(
                    "User {UpdatedByUserId} attempted to update role {RoleName} with privileged permissions",
                    updatedByUserId, role.Name);
                return null;
            }

            role.Description = roleDto.Description;
            role.Priority = roleDto.Priority;
            role.IsActive = roleDto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to update role {RoleName}: {Errors}",
                    role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                return null;
            }

            // Update permissions (replace existing)
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            // Deactivate existing permissions
            foreach (var existingPermission in existingPermissions)
            {
                existingPermission.IsActive = false;
            }

            // Add new permissions
            foreach (var permissionId in roleDto.PermissionIds)
            {
                var permission = await _context.Permissions.FindAsync(permissionId);
                if (permission?.IsActive == true)
                {
                    var existingAssignment = existingPermissions
                        .FirstOrDefault(ep => ep.PermissionId == permissionId);

                    if (existingAssignment != null)
                    {
                        // Reactivate existing assignment
                        existingAssignment.IsActive = true;
                        existingAssignment.GrantedAt = DateTime.UtcNow;
                        existingAssignment.GrantedByUserId = updatedByUserId;
                    }
                    else
                    {
                        // Create new assignment
                        var rolePermission = new RolePermission
                        {
                            RoleId = roleId,
                            PermissionId = permissionId,
                            GrantedByUserId = updatedByUserId
                        };
                        _context.RolePermissions.Add(rolePermission);
                    }
                }
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                updatedByUserId,
                "ROLE_UPDATED",
                "system",
                null,
                true,
                $"Role '{role.Name}' updated"
            );

            _logger.LogInformation("Role {RoleName} updated by {UpdatedByUserId}", role.Name, updatedByUserId);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", roleId);
            return null;
        }
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, Guid? deletedByUserId = null)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found for deletion", roleId);
                return false;
            }

            if (role.IsSystemRole)
            {
                _logger.LogWarning("Cannot delete system role {RoleName}", role.Name);
                return false;
            }

            // Check if any users have this role
            var usersWithRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersWithRole.Any())
            {
                _logger.LogWarning("Cannot delete role {RoleName} - users are assigned to it", role.Name);
                return false;
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                await _auditLogService.LogEventAsync(
                    deletedByUserId,
                    "ROLE_DELETED",
                    "system",
                    null,
                    true,
                    $"Role '{role.Name}' deleted"
                );

                _logger.LogInformation("Role {RoleName} deleted by {DeletedByUserId}", role.Name, deletedByUserId);
                return true;
            }

            _logger.LogWarning("Failed to delete role {RoleName}: {Errors}",
                role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return false;
        }
    }

    public async Task<Dictionary<string, IEnumerable<PermissionDto>>> GetAllPermissionsByCategoryAsync()
    {
        try
        {
            var permissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return permissions
                .GroupBy(p => p.Category ?? "Other")
                .ToDictionary(g => g.Key, g => g.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions by category");
            return new Dictionary<string, IEnumerable<PermissionDto>>();
        }
    }

    public async Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesWithPermissionsAsync()
    {
        try
        {
            // BUG-MED-008 FIX: Use AsSplitQuery for Include + ThenInclude to prevent cartesian explosion
            var roles = await _context.Set<Role>()
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AsSplitQuery()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Name)
                .ToListAsync();

            var result = new List<RoleWithPermissionsDto>();

            foreach (var role in roles)
            {
                var userCount = await _userManager.GetUsersInRoleAsync(role.Name!);

                result.Add(new RoleWithPermissionsDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    IsSystemRole = role.IsSystemRole,
                    IsActive = role.IsActive,
                    Priority = role.Priority,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt,
                    UserCount = userCount.Count,
                    Permissions = role.RolePermissions
                        .Where(rp => rp.IsActive && rp.Permission.IsActive)
                        .Select(rp => new PermissionDto
                        {
                            Id = rp.Permission.Id,
                            Name = rp.Permission.Name,
                            Description = rp.Permission.Description,
                            Category = rp.Permission.Category,
                            IsActive = rp.Permission.IsActive
                        })
                        .ToList()
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles with permissions");
            return Array.Empty<RoleWithPermissionsDto>();
        }
    }

    public async Task InitializeSystemRolesAndPermissionsAsync()
    {
        try
        {
            _logger.LogInformation("Initializing system roles and permissions");

            // Create permissions
            await CreatePermissionsAsync();

            // Create roles
            await CreateSystemRolesAsync();

            // Assign permissions to roles
            await AssignDefaultPermissionsAsync();

            _logger.LogInformation("System roles and permissions initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing system roles and permissions");
            throw;
        }
    }

    #region Private Helper Methods

    private async Task CreatePermissionsAsync()
    {
        foreach (var (category, permissions) in PermissionNames.ByCategory)
        {
            foreach (var permissionName in permissions)
            {
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permissionName);

                if (existingPermission == null)
                {
                    var permission = new Permission
                    {
                        Name = permissionName,
                        Category = category,
                        Description = GetPermissionDescription(permissionName),
                        IsActive = true
                    };

                    _context.Permissions.Add(permission);
                    _logger.LogDebug("Created permission: {PermissionName}", permissionName);
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateSystemRolesAsync()
    {
        var rolePriorities = new Dictionary<string, int>
        {
            [RoleNames.Admin] = 100,
            [RoleNames.Moderator] = 80,
            [RoleNames.Support] = 60,
            [RoleNames.Analyst] = 40,
            [RoleNames.User] = 20
        };

        var roleDescriptions = new Dictionary<string, string>
        {
            [RoleNames.Admin] = "System administrator with full access to all features",
            [RoleNames.Moderator] = "Content moderator with elevated permissions for community management",
            [RoleNames.Support] = "Support staff with access to user management and support tools",
            [RoleNames.Analyst] = "Read-only analyst with access to reporting and analytics",
            [RoleNames.User] = "Standard authenticated user with basic platform access"
        };

        foreach (var roleName in RoleNames.All)
        {
            var existingRole = await _roleManager.FindByNameAsync(roleName);
            if (existingRole == null)
            {
                var role = new Role(roleName)
                {
                    Description = roleDescriptions[roleName],
                    IsSystemRole = RoleNames.SystemRoles.Contains(roleName),
                    Priority = rolePriorities[roleName],
                    IsActive = true
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task AssignDefaultPermissionsAsync()
    {
        var rolePermissions = new Dictionary<string, string[]>
        {
            [RoleNames.Admin] = PermissionNames.All, // Admin gets all permissions
            [RoleNames.Moderator] = new[]
            {
                PermissionNames.ViewUsers,
                PermissionNames.EditUsers,
                PermissionNames.ViewProjects,
                PermissionNames.EditProjects,
                PermissionNames.ModerateContent,
                PermissionNames.ViewReports,
                PermissionNames.ManageReports,
                PermissionNames.ViewAnalytics
            },
            [RoleNames.Support] = new[]
            {
                PermissionNames.ViewUsers,
                PermissionNames.ViewProjects,
                PermissionNames.ViewSupportTickets,
                PermissionNames.ManageSupportTickets,
                PermissionNames.AccessSupportTools
            },
            [RoleNames.Analyst] = new[]
            {
                PermissionNames.ViewUsers,
                PermissionNames.ViewProjects,
                PermissionNames.ViewCredits,
                PermissionNames.ViewTransactionHistory,
                PermissionNames.ViewAnalytics,
                PermissionNames.ViewReports
            },
            [RoleNames.User] = new[]
            {
                PermissionNames.ViewProjects,
                PermissionNames.CreateProjects,
                PermissionNames.ViewCredits,
                PermissionNames.TransferCredits,
                PermissionNames.ViewTransactionHistory
            }
        };

        foreach (var (roleName, permissions) in rolePermissions)
        {
            foreach (var permissionName in permissions)
            {
                await AssignPermissionToRoleAsync(roleName, permissionName);
            }
        }
    }

    private string GetPermissionDescription(string permissionName)
    {
        return permissionName switch
        {
            PermissionNames.ViewUsers => "View user profiles and basic information",
            PermissionNames.CreateUsers => "Create new user accounts",
            PermissionNames.EditUsers => "Edit user profiles and account settings",
            PermissionNames.DeleteUsers => "Delete user accounts",
            PermissionNames.ManageUserRoles => "Assign and remove user roles",

            PermissionNames.ViewProjects => "View project listings and details",
            PermissionNames.CreateProjects => "Create new projects",
            PermissionNames.EditProjects => "Edit project details and settings",
            PermissionNames.DeleteProjects => "Delete projects",
            PermissionNames.ManageProjectParticipants => "Add and remove project participants",

            PermissionNames.ViewCredits => "View credit balance and history",
            PermissionNames.TransferCredits => "Transfer credits to other users",
            PermissionNames.ManageCredits => "Administrative credit management",
            PermissionNames.ViewTransactionHistory => "View credit transaction history",

            PermissionNames.ViewSystemLogs => "View system logs and audit trails",
            PermissionNames.ManageSystemSettings => "Configure system-wide settings",
            PermissionNames.ViewAnalytics => "Access analytics and reporting dashboards",
            PermissionNames.ManageRoles => "Create, edit, and delete user roles",
            PermissionNames.ManagePermissions => "Assign and revoke permissions",

            PermissionNames.ModerateContent => "Moderate user-generated content",
            PermissionNames.ViewReports => "View user reports and complaints",
            PermissionNames.ManageReports => "Process and resolve user reports",

            PermissionNames.ViewSupportTickets => "View support tickets and requests",
            PermissionNames.ManageSupportTickets => "Create, update, and close support tickets",
            PermissionNames.AccessSupportTools => "Access specialized support tools and features",

            _ => $"Permission: {permissionName}"
        };
    }

    private static bool IsPrivilegedRole(string roleName)
    {
        return PrivilegedRoles.Contains(roleName);
    }

    private static bool IsPrivilegedPermission(string permissionName)
    {
        return PrivilegedPermissions.Contains(permissionName);
    }

    private async Task<bool> ContainsPrivilegedPermissionsAsync(IEnumerable<Guid> permissionIds)
    {
        var ids = permissionIds.ToList();
        if (ids.Count == 0)
        {
            return false;
        }

        return await _context.Permissions
            .AnyAsync(p => ids.Contains(p.Id) && PrivilegedPermissions.Contains(p.Name));
    }

    private async Task<bool> IsAdminUserAsync(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return true;
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        return user != null && await _userManager.IsInRoleAsync(user, RoleNames.Admin);
    }

    #endregion
}
