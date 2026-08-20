using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Constants;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

public class SubscriptionAuthorizationService : IAuthorizationHandler
{
    private readonly ILogger<SubscriptionAuthorizationService> _logger;

    public SubscriptionAuthorizationService(ILogger<SubscriptionAuthorizationService> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements;

        foreach (var requirement in pendingRequirements.ToList())
        {
            if (requirement is SubscriptionRequirement subscriptionRequirement)
            {
                if (HasRequiredSubscription(context.User, subscriptionRequirement))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogDebug("User {UserId} does not meet subscription requirement {Requirement}",
                        context.User.Identity?.Name, subscriptionRequirement.GetType().Name);
                }
            }
        }

        return Task.CompletedTask;
    }

    private bool HasRequiredSubscription(System.Security.Claims.ClaimsPrincipal user, SubscriptionRequirement requirement)
    {
        try
        {
            // Check if trial is allowed and user is on trial FIRST
            if (requirement.AllowTrial)
            {
                var isTrialClaim = user.FindFirst(SubscriptionClaims.IsTrial)?.Value;
                if (bool.TryParse(isTrialClaim, out var isTrial) && isTrial)
                {
                    // For trial users, still check if they have a valid trial subscription status
                    var subscriptionStatusClaim = user.FindFirst(SubscriptionClaims.SubscriptionStatus)?.Value;
                    if (!string.IsNullOrEmpty(subscriptionStatusClaim) && subscriptionStatusClaim == "Trial")
                    {
                        return true;
                    }
                }
            }
            else
            {
                // BUG FIX AUTH-001: If trial is NOT allowed, block trial users immediately
                // Previously trial users with HasActiveSubscription=true could bypass paid-only features
                var isTrialClaim = user.FindFirst(SubscriptionClaims.IsTrial)?.Value;
                if (bool.TryParse(isTrialClaim, out var isTrial) && isTrial)
                {
                    return false;  // Block trial users from paid-only features
                }
            }

            // Check if user has any active subscription
            var hasActiveSubscriptionClaim = user.FindFirst(SubscriptionClaims.HasActiveSubscription)?.Value;
            if (!bool.TryParse(hasActiveSubscriptionClaim, out var hasActiveSubscription) || !hasActiveSubscription)
            {
                return false;
            }

            // Check subscription status
            var subscriptionStatusClaim2 = user.FindFirst(SubscriptionClaims.SubscriptionStatus)?.Value;
            if (string.IsNullOrEmpty(subscriptionStatusClaim2) || subscriptionStatusClaim2 == "None" || subscriptionStatusClaim2 == "Error")
            {
                return false;
            }

            // Check subscription tier
            if (requirement.RequiredTierNames?.Any() == true)
            {
                var currentTierName = user.FindFirst(SubscriptionClaims.SubscriptionTierName)?.Value;
                if (string.IsNullOrEmpty(currentTierName) || !requirement.RequiredTierNames.Contains(currentTierName))
                {
                    return false;
                }
            }

            // Check specific features
            if (requirement.RequiredFeatures?.Any() == true)
            {
                foreach (var feature in requirement.RequiredFeatures)
                {
                    if (!HasFeatureAccess(user, feature))
                    {
                        return false;
                    }
                }
            }

            // Check minimum limits
            if (requirement.MinMaxActiveProjects > 0)
            {
                var maxProjectsClaim = user.FindFirst(SubscriptionClaims.MaxActiveProjects)?.Value;
                if (!int.TryParse(maxProjectsClaim, out var maxProjects) || maxProjects < requirement.MinMaxActiveProjects)
                {
                    return false;
                }
            }

            if (requirement.MinMaxTeamMembers > 0)
            {
                var maxTeamMembersClaim = user.FindFirst(SubscriptionClaims.MaxTeamMembers)?.Value;
                if (!int.TryParse(maxTeamMembersClaim, out var maxTeamMembers) || maxTeamMembers < requirement.MinMaxTeamMembers)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subscription authorization");
            return false;
        }
    }

    private bool HasFeatureAccess(System.Security.Claims.ClaimsPrincipal user, string feature)
    {
        // Check for specific feature claims
        var featureClaim = user.FindFirst($"has_{feature.ToLower()}")?.Value;
        if (!string.IsNullOrEmpty(featureClaim) && bool.TryParse(featureClaim, out var hasFeature) && hasFeature)
        {
            return true;
        }

        // Check available features JSON array
        var availableFeaturesClaim = user.FindFirst(SubscriptionClaims.AvailableFeatures)?.Value;
        if (!string.IsNullOrEmpty(availableFeaturesClaim))
        {
            try
            {
                var availableFeatures = JsonSerializer.Deserialize<List<string>>(availableFeaturesClaim);
                return availableFeatures?.Contains(feature, StringComparer.OrdinalIgnoreCase) == true;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize available features from claims");
            }
        }

        return false;
    }
}

public class SubscriptionRequirement : IAuthorizationRequirement
{
    public List<string>? RequiredTierNames { get; set; }
    public List<string>? RequiredFeatures { get; set; }
    public bool AllowTrial { get; set; } = false;
    public int MinMaxActiveProjects { get; set; } = 0;
    public int MinMaxTeamMembers { get; set; } = 0;
}

// Specific requirement classes for common scenarios
public class ActiveSubscriptionRequirement : SubscriptionRequirement
{
    public ActiveSubscriptionRequirement()
    {
        AllowTrial = false;
    }
}

public class ActiveOrTrialSubscriptionRequirement : SubscriptionRequirement
{
    public ActiveOrTrialSubscriptionRequirement()
    {
        AllowTrial = true;
    }
}

public class BusinessOrHigherRequirement : SubscriptionRequirement
{
    public BusinessOrHigherRequirement()
    {
        RequiredTierNames = new List<string> { "Business", "Enterprise" };
        AllowTrial = true;
    }
}

public class EnterpriseTierRequirement : SubscriptionRequirement
{
    public EnterpriseTierRequirement()
    {
        RequiredTierNames = new List<string> { "Enterprise" };
        AllowTrial = false;
    }
}

public class ApiAccessRequirement : SubscriptionRequirement
{
    public ApiAccessRequirement()
    {
        RequiredFeatures = new List<string> { "api_access" };
        AllowTrial = true;
    }
}

public class PrioritySupportRequirement : SubscriptionRequirement
{
    public PrioritySupportRequirement()
    {
        RequiredFeatures = new List<string> { "priority_support" };
        AllowTrial = true;
    }
}

public class AdvancedAnalyticsRequirement : SubscriptionRequirement
{
    public AdvancedAnalyticsRequirement()
    {
        RequiredFeatures = new List<string> { "advanced_analytics" };
        AllowTrial = false;
    }
}

public class MultiSignatureRequirement : SubscriptionRequirement
{
    public MultiSignatureRequirement()
    {
        RequiredFeatures = new List<string> { "multi_signature" };
        AllowTrial = false;
    }
}

public class CustomIntegrationsRequirement : SubscriptionRequirement
{
    public CustomIntegrationsRequirement()
    {
        RequiredFeatures = new List<string> { "custom_integrations" };
        AllowTrial = false;
    }
}

public class AdvancedFraudDetectionRequirement : SubscriptionRequirement
{
    public AdvancedFraudDetectionRequirement()
    {
        RequiredFeatures = new List<string> { "advanced_fraud_detection" };
        AllowTrial = false;
    }
}

public class TeamMemberAccessRequirement : SubscriptionRequirement
{
    public TeamMemberAccessRequirement()
    {
        MinMaxTeamMembers = 2;
        AllowTrial = true;
    }
}

public class UnlimitedProjectsRequirement : SubscriptionRequirement
{
    public UnlimitedProjectsRequirement()
    {
        MinMaxActiveProjects = 999;
        AllowTrial = false;
    }
}