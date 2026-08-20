using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for permission-based requirements
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly SkillLedger.Core.Interfaces.IAuthorizationService _authorizationService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        SkillLedger.Core.Interfaces.IAuthorizationService authorizationService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogDebug("Authorization failed: Invalid or missing user ID claim");
                context.Fail();
                return;
            }

            // Check permissions based on requirement type
            bool hasPermission = requirement.RequireAll
                ? await _authorizationService.HasAllPermissionsAsync(userId, requirement.Permissions)
                : await _authorizationService.HasAnyPermissionAsync(userId, requirement.Permissions);

            if (hasPermission)
            {
                _logger.LogDebug("Authorization succeeded for user {UserId} with permissions {Permissions} (RequireAll: {RequireAll})",
                    userId, string.Join(", ", requirement.Permissions), requirement.RequireAll);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogDebug("Authorization failed for user {UserId}. Required permissions: {Permissions} (RequireAll: {RequireAll})",
                    userId, string.Join(", ", requirement.Permissions), requirement.RequireAll);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission authorization for user {UserId}",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            context.Fail();
        }
    }
}

/// <summary>
/// Authorization requirement for permission-based access
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Required permissions
    /// </summary>
    public string[] Permissions { get; }

    /// <summary>
    /// Whether all permissions are required (AND) or any (OR)
    /// </summary>
    public bool RequireAll { get; }

    /// <summary>
    /// Initialize with permission requirements
    /// </summary>
    /// <param name="permissions">Required permissions</param>
    /// <param name="requireAll">Whether all permissions are required (AND) or any (OR)</param>
    public PermissionRequirement(string[] permissions, bool requireAll = true)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        RequireAll = requireAll;
    }

    /// <summary>
    /// Initialize with a single permission requirement
    /// </summary>
    /// <param name="permission">Required permission</param>
    public PermissionRequirement(string permission) : this(new[] { permission }, true)
    {
    }
}