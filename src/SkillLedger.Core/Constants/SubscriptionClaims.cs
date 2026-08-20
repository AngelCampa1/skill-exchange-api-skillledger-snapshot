namespace SkillLedger.Core.Constants;

/// <summary>
/// Constants for subscription-related claims
/// </summary>
public static class SubscriptionClaims
{
    /// <summary>
    /// Indicates if user has an active subscription
    /// </summary>
    public const string HasActiveSubscription = "has_active_subscription";

    /// <summary>
    /// Current subscription tier ID
    /// </summary>
    public const string SubscriptionTierId = "subscription_tier_id";

    /// <summary>
    /// Current subscription tier name
    /// </summary>
    public const string SubscriptionTierName = "subscription_tier_name";

    /// <summary>
    /// Subscription status
    /// </summary>
    public const string SubscriptionStatus = "subscription_status";

    /// <summary>
    /// Subscription start date
    /// </summary>
    public const string SubscriptionStartDate = "subscription_start_date";

    /// <summary>
    /// Subscription end date
    /// </summary>
    public const string SubscriptionEndDate = "subscription_end_date";

    /// <summary>
    /// Next billing date
    /// </summary>
    public const string NextBillingDate = "next_billing_date";

    /// <summary>
    /// Trial end date
    /// </summary>
    public const string TrialEndDate = "trial_end_date";

    /// <summary>
    /// Whether user has priority support
    /// </summary>
    public const string HasPrioritySupport = "has_priority_support";

    /// <summary>
    /// Whether user has API access
    /// </summary>
    public const string HasApiAccess = "has_api_access";

    /// <summary>
    /// Whether user has advanced analytics
    /// </summary>
    public const string HasAdvancedAnalytics = "has_advanced_analytics";

    /// <summary>
    /// Whether user has advanced fraud detection
    /// </summary>
    public const string HasAdvancedFraudDetection = "has_advanced_fraud_detection";

    /// <summary>
    /// Whether user has multi-signature access
    /// </summary>
    public const string HasMultiSignature = "has_multi_signature";

    /// <summary>
    /// Whether user has custom integrations
    /// </summary>
    public const string HasCustomIntegrations = "has_custom_integrations";

    /// <summary>
    /// Maximum active projects allowed
    /// </summary>
    public const string MaxActiveProjects = "max_active_projects";

    /// <summary>
    /// Maximum team members allowed
    /// </summary>
    public const string MaxTeamMembers = "max_team_members";

    /// <summary>
    /// Maximum monthly earnings allowed
    /// </summary>
    public const string MaxMonthlyEarnings = "max_monthly_earnings";

    /// <summary>
    /// Credit bonus per billing cycle
    /// </summary>
    public const string CreditBonus = "credit_bonus";

    /// <summary>
    /// Whether user is on trial
    /// </summary>
    public const string IsTrial = "is_trial";

    /// <summary>
    /// Subscription billing cycle count
    /// </summary>
    public const string BillingCycleCount = "billing_cycle_count";

    /// <summary>
    /// Whether subscription auto-renews
    /// </summary>
    public const string AutoRenew = "auto_renew";

    /// <summary>
    /// Whether this is an annual subscription
    /// </summary>
    public const string IsAnnual = "is_annual";

    /// <summary>
    /// Available features as JSON array
    /// </summary>
    public const string AvailableFeatures = "available_features";
}

/// <summary>
/// Authorization policy names for subscription-based access control
/// </summary>
public static class SubscriptionPolicies
{
    /// <summary>
    /// Users with active subscription
    /// </summary>
    public const string ActiveSubscription = "ActiveSubscription";

    /// <summary>
    /// Users with business tier or higher
    /// </summary>
    public const string BusinessOrHigher = "BusinessOrHigher";

    /// <summary>
    /// Users with enterprise tier
    /// </summary>
    public const string EnterpriseTier = "EnterpriseTier";

    /// <summary>
    /// Users with priority support
    /// </summary>
    public const string PrioritySupport = "PrioritySupport";

    /// <summary>
    /// Users with API access
    /// </summary>
    public const string ApiAccess = "ApiAccess";

    /// <summary>
    /// Users with advanced analytics
    /// </summary>
    public const string AdvancedAnalytics = "AdvancedAnalytics";

    /// <summary>
    /// Users with advanced fraud detection
    /// </summary>
    public const string AdvancedFraudDetection = "AdvancedFraudDetection";

    /// <summary>
    /// Users with multi-signature access
    /// </summary>
    public const string MultiSignature = "MultiSignature";

    /// <summary>
    /// Users with custom integrations
    /// </summary>
    public const string CustomIntegrations = "CustomIntegrations";

    /// <summary>
    /// Users on trial
    /// </summary>
    public const string TrialUsers = "TrialUsers";

    /// <summary>
    /// Users not on trial
    /// </summary>
    public const string PaidUsers = "PaidUsers";

    /// <summary>
    /// Users with unlimited projects
    /// </summary>
    public const string UnlimitedProjects = "UnlimitedProjects";

    /// <summary>
    /// Users with team member access
    /// </summary>
    public const string TeamMemberAccess = "TeamMemberAccess";

    /// <summary>
    /// Feature-specific policies
    /// </summary>
    public const string FeaturePrefix = "Feature_";
}