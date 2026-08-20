using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for seeding subscription tiers and initial subscription data
/// </summary>
public class SubscriptionDataSeeder
{
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<SubscriptionDataSeeder> _logger;

    public SubscriptionDataSeeder(
        SkillLedgerDbContext context,
        ILogger<SubscriptionDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds subscription tiers if they don't exist
    /// </summary>
    public async Task SeedSubscriptionTiersAsync()
    {
        try
        {
            var existingTiers = await _context.SubscriptionTiers.ToListAsync();

            if (existingTiers.Any())
            {
                _logger.LogInformation("Subscription tiers already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Starting subscription tiers seeding...");

            var tiers = new List<SubscriptionTier>
            {
                // Professional Tier - $19/month
                new SubscriptionTier
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Type = SubscriptionTierType.Professional,
                    Name = "Professional",
                    Description = "Perfect for freelancers and small teams starting out",
                    Price = 19.00m,
                    AnnualPrice = 190.00m, // 2 months free (20% discount)
                    CreditBonus = 100,
                    MaxActiveProjects = 5,
                    MaxTeamMembers = 1,
                    PrioritySupport = false,
                    ApiAccess = false,
                    AdvancedAnalytics = false,
                    AdvancedFraudDetection = false,
                    MultiSignature = false,
                    CustomIntegrations = false,
                    MaxMonthlyEarnings = 5000,
                    Features = JsonSerializer.Serialize(new List<string>
                    {
                        "basic_project_management",
                        "credit_wallet",
                        "messaging",
                        "file_sharing",
                        "basic_analytics"
                    }),
                    IsActive = true,
                    SortOrder = 1
                },

                // Business Tier - $49/month
                new SubscriptionTier
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Type = SubscriptionTierType.Business,
                    Name = "Business",
                    Description = "Ideal for growing businesses and agencies",
                    Price = 49.00m,
                    AnnualPrice = 490.00m, // 2 months free (20% discount)
                    CreditBonus = 500,
                    MaxActiveProjects = 25,
                    MaxTeamMembers = 10,
                    PrioritySupport = true,
                    ApiAccess = true,
                    AdvancedAnalytics = true,
                    AdvancedFraudDetection = false,
                    MultiSignature = false,
                    CustomIntegrations = false,
                    MaxMonthlyEarnings = 25000,
                    Features = JsonSerializer.Serialize(new List<string>
                    {
                        "advanced_project_management",
                        "priority_support",
                        "api_access",
                        "advanced_analytics",
                        "team_collaboration",
                        "custom_workflows",
                        "priority_messaging",
                        "advanced_file_sharing",
                        "performance_analytics",
                        "export_reports"
                    }),
                    IsActive = true,
                    SortOrder = 2
                },

                // Enterprise Tier - $99/month
                new SubscriptionTier
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Type = SubscriptionTierType.Enterprise,
                    Name = "Enterprise",
                    Description = "Complete solution for large organizations",
                    Price = 99.00m,
                    AnnualPrice = 990.00m, // 2 months free (20% discount)
                    CreditBonus = 2000,
                    MaxActiveProjects = 9999, // Unlimited
                    MaxTeamMembers = 9999, // Unlimited
                    PrioritySupport = true,
                    ApiAccess = true,
                    AdvancedAnalytics = true,
                    AdvancedFraudDetection = true,
                    MultiSignature = true,
                    CustomIntegrations = true,
                    MaxMonthlyEarnings = 999999999, // Unlimited
                    Features = JsonSerializer.Serialize(new List<string>
                    {
                        "enterprise_project_management",
                        "white_label_options",
                        "advanced_fraud_detection",
                        "multi_signature_transactions",
                        "custom_integrations",
                        "dedicated_account_manager",
                        "sla_guarantee",
                        "custom_workflows",
                        "advanced_compliance",
                        "audit_logs",
                        "custom_analytics",
                        "api_rate_limits_high",
                        "priority_queue",
                        "custom_reporting",
                        "data_export_api",
                        "integration_support"
                    }),
                    IsActive = true,
                    SortOrder = 3
                }
            };

            await _context.SubscriptionTiers.AddRangeAsync(tiers);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} subscription tiers", tiers.Count);

            // Log tier details for verification
            foreach (var tier in tiers)
            {
                _logger.LogInformation("Created tier: {Name} (${Price}/month, {Credits} credits, {Projects} projects)",
                    tier.Name, tier.Price, tier.CreditBonus,
                    tier.MaxActiveProjects == -1 ? "Unlimited" : tier.MaxActiveProjects.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding subscription tiers");
            throw;
        }
    }

    /// <summary>
    /// Validates that subscription tiers are properly configured
    /// </summary>
    public async Task<bool> ValidateSubscriptionTiersAsync()
    {
        try
        {
            var tiers = await _context.SubscriptionTiers
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync();

            if (!tiers.Any())
            {
                _logger.LogWarning("No active subscription tiers found");
                return false;
            }

            var validationErrors = new List<string>();

            // Check for required tiers
            var requiredTypes = new[] { SubscriptionTierType.Professional, SubscriptionTierType.Business, SubscriptionTierType.Enterprise };
            foreach (var requiredType in requiredTypes)
            {
                if (!tiers.Any(t => t.Type == requiredType))
                {
                    validationErrors.Add($"Missing required tier type: {requiredType}");
                }
            }

            // Validate pricing structure
            foreach (var tier in tiers)
            {
                if (tier.Price <= 0)
                {
                    validationErrors.Add($"Tier {tier.Name} has invalid price: {tier.Price}");
                }

                if (tier.AnnualPrice.HasValue && tier.AnnualPrice >= tier.Price * 12)
                {
                    validationErrors.Add($"Tier {tier.Name} annual price should be less than 12x monthly price");
                }

                if (string.IsNullOrEmpty(tier.Features))
                {
                    validationErrors.Add($"Tier {tier.Name} has no features defined");
                }
            }

            if (validationErrors.Any())
            {
                _logger.LogError("Subscription tier validation failed: {Errors}",
                    string.Join(", ", validationErrors));
                return false;
            }

            _logger.LogInformation("Subscription tier validation passed for {Count} tiers", tiers.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription tiers");
            return false;
        }
    }

    /// <summary>
    /// Gets subscription tier details for display
    /// </summary>
    public async Task<List<SubscriptionTierDisplayDto>> GetSubscriptionTiersForDisplayAsync()
    {
        try
        {
            var tiers = await _context.SubscriptionTiers
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync();

            return tiers.Select(tier => new SubscriptionTierDisplayDto
            {
                Id = tier.Id,
                Name = tier.Name,
                Description = tier.Description,
                Price = tier.Price,
                AnnualPrice = tier.AnnualPrice,
                CreditBonus = tier.CreditBonus,
                MaxActiveProjects = tier.MaxActiveProjects,
                MaxTeamMembers = tier.MaxTeamMembers,
                PrioritySupport = tier.PrioritySupport,
                ApiAccess = tier.ApiAccess,
                AdvancedAnalytics = tier.AdvancedAnalytics,
                AdvancedFraudDetection = tier.AdvancedFraudDetection,
                MultiSignature = tier.MultiSignature,
                CustomIntegrations = tier.CustomIntegrations,
                MaxMonthlyEarnings = tier.MaxMonthlyEarnings,
                Features = string.IsNullOrEmpty(tier.Features)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(tier.Features) ?? new List<string>(),
                AnnualSavings = tier.AnnualPrice.HasValue
                    ? (int)Math.Round(((tier.Price * 12 - tier.AnnualPrice.Value) / (tier.Price * 12)) * 100, 0)
                    : 0,
                MostPopular = tier.Type == SubscriptionTierType.Business
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription tiers for display");
            return new List<SubscriptionTierDisplayDto>();
        }
    }
}

/// <summary>
/// DTO for displaying subscription tier information
/// </summary>
public class SubscriptionTierDisplayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? AnnualPrice { get; set; }
    public int CreditBonus { get; set; }
    public int MaxActiveProjects { get; set; }
    public int MaxTeamMembers { get; set; }
    public bool PrioritySupport { get; set; }
    public bool ApiAccess { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool AdvancedFraudDetection { get; set; }
    public bool MultiSignature { get; set; }
    public bool CustomIntegrations { get; set; }
    public int MaxMonthlyEarnings { get; set; }
    public List<string> Features { get; set; } = new();
    public int AnnualSavings { get; set; }
    public bool MostPopular { get; set; }
}