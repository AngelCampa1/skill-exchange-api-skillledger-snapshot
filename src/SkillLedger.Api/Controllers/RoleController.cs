using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.Attributes;
using SkillLedger.Core.Constants;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for managing roles and permissions in the RBAC system
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("RoleManagementPolicy")]
public class RoleController : ControllerBase
{
    private readonly SkillLedger.Core.Interfaces.IAuthorizationService _authorizationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(
        SkillLedger.Core.Interfaces.IAuthorizationService authorizationService,
        IAuditLogService auditLogService,
        ILogger<RoleController> logger)
    {
        _authorizationService = authorizationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles with their permissions
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionNames.ManageRoles)]
    public async Task<ActionResult<IEnumerable<RoleWithPermissionsDto>>> GetRoles()
    {
        try
        {
            var roles = await _authorizationService.GetAllRolesWithPermissionsAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new { Message = "An error occurred while retrieving roles" });
        }
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionNames.ManageRoles)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto roleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var role = await _authorizationService.CreateRoleAsync(roleDto, currentUserId);

            if (role == null)
            {
                return BadRequest(new { Message = "Failed to create role. Role may already exist." });
            }

            var roleResponse = new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                IsActive = role.IsActive,
                Priority = role.Priority,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                UserCount = 0
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", roleDto.Name);
            return StatusCode(500, new { Message = "An error occurred while creating the role" });
        }
    }

    /// <summary>
    /// Get a specific role by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionNames.ManageRoles)]
    public async Task<ActionResult<RoleWithPermissionsDto>> GetRole(Guid id)
    {
        try
        {
            var roles = await _authorizationService.GetAllRolesWithPermissionsAsync();
            var role = roles.FirstOrDefault(r => r.Id == id);

            if (role == null)
            {
                return NotFound(new { Message = "Role not found" });
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the role" });
        }
    }

    /// <summary>
    /// Update a role
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionNames.ManageRoles)]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleDto roleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var role = await _authorizationService.UpdateRoleAsync(id, roleDto, currentUserId);

            if (role == null)
            {
                return NotFound(new { Message = "Role not found or cannot be modified" });
            }

            var roleResponse = new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                IsActive = role.IsActive,
                Priority = role.Priority,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                UserCount = 0 // Would need additional query to get accurate count
            };

            return Ok(roleResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating the role" });
        }
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionNames.ManageRoles)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _authorizationService.DeleteRoleAsync(id, currentUserId);

            if (!success)
            {
                return BadRequest(new { Message = "Role cannot be deleted. It may be a system role or have users assigned to it." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting the role" });
        }
    }

    /// <summary>
    /// Get all available permissions grouped by category
    /// </summary>
    [HttpGet("permissions")]
    [RequireAnyPermission(PermissionNames.ManageRoles, PermissionNames.ManagePermissions)]
    public async Task<ActionResult<Dictionary<string, IEnumerable<PermissionDto>>>> GetPermissions()
    {
        try
        {
            var permissions = await _authorizationService.GetAllPermissionsByCategoryAsync();
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return StatusCode(500, new { Message = "An error occurred while retrieving permissions" });
        }
    }

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    [HttpPost("assign")]
    [RequirePermission(PermissionNames.ManageUserRoles)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _authorizationService.AssignRoleAsync(
                assignRoleDto.UserId,
                assignRoleDto.RoleName,
                currentUserId);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to assign role. User or role may not exist." });
            }

            return Ok(new { Message = $"Role '{assignRoleDto.RoleName}' successfully assigned to user" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}",
                assignRoleDto.RoleName, assignRoleDto.UserId);
            return StatusCode(500, new { Message = "An error occurred while assigning the role" });
        }
    }

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    [HttpPost("unassign")]
    [RequirePermission(PermissionNames.ManageUserRoles)]
    public async Task<IActionResult> UnassignRole([FromBody] AssignRoleDto assignRoleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _authorizationService.RemoveRoleAsync(
                assignRoleDto.UserId,
                assignRoleDto.RoleName,
                currentUserId);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to remove role. User or role may not exist." });
            }

            return Ok(new { Message = $"Role '{assignRoleDto.RoleName}' successfully removed from user" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}",
                assignRoleDto.RoleName, assignRoleDto.UserId);
            return StatusCode(500, new { Message = "An error occurred while removing the role" });
        }
    }

    /// <summary>
    /// Assign a permission to a role
    /// </summary>
    [HttpPost("permissions/assign")]
    [RequirePermission(PermissionNames.ManagePermissions)]
    public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionDto assignPermissionDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _authorizationService.AssignPermissionToRoleAsync(
                assignPermissionDto.RoleName,
                assignPermissionDto.PermissionName,
                currentUserId);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to assign permission. Role or permission may not exist." });
            }

            return Ok(new { Message = $"Permission '{assignPermissionDto.PermissionName}' successfully assigned to role '{assignPermissionDto.RoleName}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionName} to role {RoleName}",
                assignPermissionDto.PermissionName, assignPermissionDto.RoleName);
            return StatusCode(500, new { Message = "An error occurred while assigning the permission" });
        }
    }

    /// <summary>
    /// Remove a permission from a role
    /// </summary>
    [HttpPost("permissions/unassign")]
    [RequirePermission(PermissionNames.ManagePermissions)]
    public async Task<IActionResult> UnassignPermission([FromBody] AssignPermissionDto assignPermissionDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _authorizationService.RemovePermissionFromRoleAsync(
                assignPermissionDto.RoleName,
                assignPermissionDto.PermissionName,
                currentUserId);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to remove permission. Role or permission may not exist." });
            }

            return Ok(new { Message = $"Permission '{assignPermissionDto.PermissionName}' successfully removed from role '{assignPermissionDto.RoleName}'" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionName} from role {RoleName}",
                assignPermissionDto.PermissionName, assignPermissionDto.RoleName);
            return StatusCode(500, new { Message = "An error occurred while removing the permission" });
        }
    }

    /// <summary>
    /// Get current user's roles and permissions
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<ActionResult<UserWithRolesDto>> GetMyPermissions()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var roles = await _authorizationService.GetUserRolesAsync(currentUserId.Value);
            var permissions = await _authorizationService.GetUserPermissionsAsync(currentUserId.Value);

            // Get current user info from claims
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            var userWithRoles = new UserWithRolesDto
            {
                Id = currentUserId.Value,
                Email = email,
                UserName = userName,
                Status = User.FindFirst("user_status")?.Value ?? "",
                Roles = roles.Select(r => new RoleDto { Name = r }).ToList(),
                Permissions = permissions.Select(p => new PermissionDto { Name = p }).ToList()
            };

            return Ok(userWithRoles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions for current user");
            return StatusCode(500, new { Message = "An error occurred while retrieving your permissions" });
        }
    }

    #region Private Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #endregion
}