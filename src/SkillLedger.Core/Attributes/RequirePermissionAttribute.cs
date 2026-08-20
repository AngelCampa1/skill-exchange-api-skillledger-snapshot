using Microsoft.AspNetCore.Authorization;

namespace SkillLedger.Core.Attributes;

/// <summary>
/// Authorization attribute that requires specific permissions to access an action or controller
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Required permission name
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Whether to require all specified permissions (AND logic) or any (OR logic)
    /// </summary>
    public bool RequireAll { get; set; } = true;

    /// <summary>
    /// Initialize with a single permission requirement
    /// </summary>
    /// <param name="permission">Required permission name</param>
    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"RequirePermission:{permission}";
    }

    /// <summary>
    /// Initialize with multiple permission requirements
    /// </summary>
    /// <param name="permissions">Required permission names</param>
    public RequirePermissionAttribute(params string[] permissions)
    {
        Permission = string.Join(",", permissions);
        var logicOperator = RequireAll ? "AND" : "OR";
        Policy = $"RequirePermissions:{logicOperator}:{string.Join(",", permissions)}";
    }
}

/// <summary>
/// Authorization attribute that requires any of the specified permissions (OR logic)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAnyPermissionAttribute : RequirePermissionAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissions) : base(permissions)
    {
        RequireAll = false;
        Policy = $"RequirePermissions:OR:{string.Join(",", permissions)}";
    }
}

/// <summary>
/// Authorization attribute that requires all of the specified permissions (AND logic)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAllPermissionsAttribute : RequirePermissionAttribute
{
    public RequireAllPermissionsAttribute(params string[] permissions) : base(permissions)
    {
        RequireAll = true;
        Policy = $"RequirePermissions:AND:{string.Join(",", permissions)}";
    }
}