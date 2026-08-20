using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Services;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SkillLedger.Api.Middleware;

/// <summary>
/// Middleware to enforce subscription-based access control
/// </summary>
public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SubscriptionMiddleware(RequestDelegate next, ILogger<SubscriptionMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware for static files, health checks, etc.
        if (ShouldSkipMiddleware(context))
        {
            await _next(context);
            return;
        }

        // Extract user ID from JWT token — only authenticated users are subject to subscription enforcement
        var userId = GetUserIdFromContext(context);

        if (userId.HasValue)
        {
            var endpoint = context.GetEndpoint();

            // [SubscriptionExempt] bypasses subscription enforcement for this endpoint
            var isExempt = endpoint?.Metadata?.Any(m => m is SubscriptionExemptAttribute) ?? false;

            // Default-on: all authenticated requests require an active subscription
            // unless the endpoint is explicitly marked [SubscriptionExempt]
            if (!isExempt)
            {
                var validationResult = await ValidateSubscriptionAccessAsync(context, userId.Value, endpoint);

                if (!validationResult.IsAuthorized)
                {
                    _logger.LogWarning("User {UserId} denied access to {Path}: {Reason}",
                        userId.Value, context.Request.Path, validationResult.Reason);

                    context.Response.StatusCode = validationResult.StatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        error = validationResult.ErrorType,
                        message = validationResult.Message,
                        requiredTier = validationResult.RequiredTier,
                        currentTier = validationResult.CurrentTier,
                        upgradeUrl = validationResult.UpgradeUrl
                    }));
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool ShouldSkipMiddleware(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip static files
        if (path.StartsWith("/static/") ||
            path.StartsWith("/css/") ||
            path.StartsWith("/js/") ||
            path.StartsWith("/images/") ||
            path.StartsWith("/favicon") ||
            path.EndsWith(".css") ||
            path.EndsWith(".js") ||
            path.EndsWith(".png") ||
            path.EndsWith(".jpg") ||
            path.EndsWith(".gif") ||
            path.EndsWith(".ico"))
        {
            return true;
        }

        // Skip health checks
        if (path.StartsWith("/health") ||
            path.StartsWith("/api/webhook") ||
            path.StartsWith("/.well-known/"))
        {
            return true;
        }

        // Skip auth endpoints (people need to register/login first)
        if (path.StartsWith("/api/auth/") ||
            path.StartsWith("/api/subscriptiontier/") ||
            path.StartsWith("/register") ||
            path.StartsWith("/login") ||
            path.StartsWith("/forgot-password"))
        {
            return true;
        }

        // Skip subscription-view endpoints (unauthenticated users need to see tier listings)
        if (path.StartsWith("/api/subscription/"))
        {
            return true;
        }

        return false;
    }

    private static Guid? GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    private async Task<SubscriptionValidationResult> ValidateSubscriptionAccessAsync(HttpContext context, Guid userId, Endpoint? endpoint)
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        // Check if user has active subscription
        var subscription = await subscriptionService.GetUserActiveSubscriptionAsync(userId);

        if (subscription == null)
        {
            // No active subscription found
            var requiredTier = GetTierRequirementFromEndpoint(endpoint);
            return new SubscriptionValidationResult
            {
                IsAuthorized = false,
                StatusCode = StatusCodes.Status402PaymentRequired,
                ErrorType = "SUBSCRIPTION_REQUIRED",
                Message = "An active subscription is required to access this feature",
                RequiredTier = requiredTier,
                CurrentTier = "None",
                UpgradeUrl = GetUpgradeUrl(requiredTier)
            };
        }

        // Check for trial expiration
        if (subscription.Status == SubscriptionStatus.Trial &&
            subscription.TrialEndDate.HasValue &&
            subscription.TrialEndDate.Value < DateTime.UtcNow)
        {
            // Trial expired
            var requiredTier = GetTierRequirementFromEndpoint(endpoint);
            return new SubscriptionValidationResult
            {
                IsAuthorized = false,
                StatusCode = StatusCodes.Status402PaymentRequired,
                ErrorType = "TRIAL_EXPIRED",
                Message = "Your free trial has expired. Please upgrade to continue.",
                RequiredTier = requiredTier,
                CurrentTier = "Trial",
                UpgradeUrl = GetUpgradeUrl(requiredTier)
            };
        }

        // Check for past due status
        if (subscription.Status == SubscriptionStatus.PastDue)
        {
            var requiredTier = GetTierRequirementFromEndpoint(endpoint);
            return new SubscriptionValidationResult
            {
                IsAuthorized = false,
                StatusCode = StatusCodes.Status402PaymentRequired,
                ErrorType = "PAYMENT_REQUIRED",
                Message = "Payment is required. Please update your payment method to continue.",
                RequiredTier = requiredTier,
                CurrentTier = "PastDue",
                UpgradeUrl = GetUpgradeUrl(requiredTier)
            };
        }

        // Check for specific feature access
        var featureRequired = GetRequiredFeatureFromEndpoint(endpoint);
        if (!string.IsNullOrEmpty(featureRequired))
        {
            var hasFeatureAccess = await subscriptionService.HasFeatureAccessAsync(userId, featureRequired);
            if (!hasFeatureAccess)
            {
                var requiredTier = GetTierRequirementFromEndpoint(endpoint);
                return new SubscriptionValidationResult
                {
                    IsAuthorized = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    ErrorType = "FEATURE_NOT_AVAILABLE",
                    Message = $"The '{featureRequired}' feature is not available with your current subscription tier.",
                    RequiredTier = requiredTier,
                    CurrentTier = subscription.SubscriptionTier?.Name ?? "Unknown",
                    UpgradeUrl = GetUpgradeUrl(requiredTier)
                };
            }
        }

        // Check tier-specific requirements
        var tierRequirement = GetTierRequirementFromEndpoint(endpoint);
        if (tierRequirement != null)
        {
            var currentTierName = subscription.SubscriptionTier?.Name?.ToLower();
            var isValidTier = tierRequirement switch
            {
                "professional" => currentTierName == "professional" || currentTierName == "business" || currentTierName == "enterprise",
                "business" => currentTierName == "business" || currentTierName == "enterprise",
                "enterprise" => currentTierName == "enterprise",
                _ => true
            };

            if (!isValidTier)
            {
                return new SubscriptionValidationResult
                {
                    IsAuthorized = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    ErrorType = "TIER_NOT_SUFFICIENT",
                    Message = $"This feature requires a {tierRequirement} subscription or higher.",
                    RequiredTier = tierRequirement,
                    CurrentTier = currentTierName ?? "Unknown",
                    UpgradeUrl = GetUpgradeUrl(tierRequirement)
                };
            }
        }

        // Check project limits
        if (subscription.SubscriptionTier != null)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SkillLedger.Infrastructure.Data.SkillLedgerDbContext>();
            var userProjects = await GetUserProjectCount(userId, dbContext);
            if (userProjects >= subscription.SubscriptionTier.MaxActiveProjects &&
                endpoint.Metadata?.Any(m => m is ProjectLimitAttribute) == true)
            {
                return new SubscriptionValidationResult
                {
                    IsAuthorized = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    ErrorType = "PROJECT_LIMIT_REACHED",
                    Message = $"You have reached your project limit of {subscription.SubscriptionTier.MaxActiveProjects} projects. Upgrade to create more projects.",
                    RequiredTier = subscription.SubscriptionTier.Name,
                    CurrentTier = subscription.SubscriptionTier.Name,
                    UpgradeUrl = GetUpgradeUrl(subscription.SubscriptionTier.Name)
                };
            }
        }

        return new SubscriptionValidationResult
        {
            IsAuthorized = true,
            StatusCode = StatusCodes.Status200OK,
            CurrentTier = subscription.SubscriptionTier?.Name ?? "Unknown"
        };
    }

    private static string? GetRequiredFeatureFromEndpoint(Endpoint endpoint)
    {
        var attribute = endpoint?.Metadata?.FirstOrDefault(m => m is FeatureRequiredAttribute) as FeatureRequiredAttribute;
        return attribute?.FeatureName;
    }

    private static string? GetTierRequirementFromEndpoint(Endpoint endpoint)
    {
        var attribute = endpoint?.Metadata?.FirstOrDefault(m => m is TierRequiredAttribute) as TierRequiredAttribute;
        return attribute?.RequiredTier?.ToLower();
    }

    private static string GetUpgradeUrl(string? requiredTier)
    {
        return $"/pricing?tier={requiredTier?.ToLower() ?? "basic"}";
    }

    private static async Task<int> GetUserProjectCount(Guid userId, SkillLedger.Infrastructure.Data.SkillLedgerDbContext context)
    {
        return await context.Projects
            .CountAsync(p => p.ClientId == userId &&
                (p.Status == Core.Enums.ProjectStatus.Published ||
                 p.Status == Core.Enums.ProjectStatus.InProgress));
    }
}

/// <summary>
/// Validation result for subscription access
/// </summary>
public class SubscriptionValidationResult
{
    public bool IsAuthorized { get; set; }
    public int StatusCode { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Reason => ErrorType; // Alias for backward compatibility
    public string? RequiredTier { get; set; }
    public string? CurrentTier { get; set; }
    public string? UpgradeUrl { get; set; }
}

/// <summary>
/// Attribute to mark endpoints that require an active subscription
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SubscriptionRequiredAttribute : Attribute
{
}

/// <summary>
/// Attribute to mark endpoints that require a specific subscription tier
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TierRequiredAttribute : Attribute
{
    public string RequiredTier { get; }

    public TierRequiredAttribute(string requiredTier)
    {
        RequiredTier = requiredTier;
    }
}

/// <summary>
/// Attribute to mark endpoints that require a specific feature
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FeatureRequiredAttribute : Attribute
{
    public string FeatureName { get; }

    public FeatureRequiredAttribute(string featureName)
    {
        FeatureName = featureName;
    }
}

/// <summary>
/// Attribute to mark endpoints that check project limits
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ProjectLimitAttribute : Attribute
{
}

/// <summary>
/// Attribute to mark endpoints that are exempt from the default-on subscription enforcement.
/// Endpoints with this attribute are accessible to authenticated users regardless of subscription status.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SubscriptionExemptAttribute : Attribute
{
}