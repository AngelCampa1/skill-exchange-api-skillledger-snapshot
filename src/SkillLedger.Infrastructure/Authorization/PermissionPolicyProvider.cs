using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SkillLedger.Infrastructure.Authorization;

/// <summary>
/// Dynamic authorization policy provider for permission-based policies
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle single permission policies: "RequirePermission:{permission}"
        if (policyName.StartsWith("RequirePermission:", StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName.Substring("RequirePermission:".Length);
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Handle multiple permission policies: "RequirePermissions:{AND|OR}:{permission1,permission2,...}"
        if (policyName.StartsWith("RequirePermissions:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName.Split(':', 3);
            if (parts.Length == 3)
            {
                var logicOperator = parts[1].ToUpperInvariant();
                var permissions = parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries);
                var requireAll = logicOperator == "AND";

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permissions, requireAll))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        // Fall back to default provider for other policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}