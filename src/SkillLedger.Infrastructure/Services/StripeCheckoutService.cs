using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Stripe Checkout service for creating subscription purchase sessions
/// </summary>
public class StripeCheckoutService
{
    private readonly ILogger<StripeCheckoutService> _logger;
    private readonly StripeSettings _stripeSettings;
    private readonly SkillLedgerDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;

    public StripeCheckoutService(
        ILogger<StripeCheckoutService> logger,
        IOptions<StripeSettings> stripeSettings,
        SkillLedgerDbContext context,
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _stripeSettings = stripeSettings.Value;
        _context = context;
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;

        // Configure Stripe
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    /// <summary>
    /// Creates a Stripe Checkout session for a new subscription
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tierId">Subscription tier ID</param>
    /// <param name="billingCycle">Monthly or Annual billing</param>
    /// <param name="successUrl">URL to redirect to on successful payment</param>
    /// <param name="cancelUrl">URL to redirect to on cancelled payment</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Checkout session details</returns>
    public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(
        Guid userId,
        Guid tierId,
        BillingCycle billingCycle,
        string successUrl,
        string cancelUrl,
        string? createdFromIP = null,
        int? trialPeriodDays = null)
    {
        try
        {
            // Validate subscription tier exists and is active
            var tier = await _context.SubscriptionTiers
                .FirstOrDefaultAsync(t => t.Id == tierId);

            if (tier == null)
            {
                await _auditLogService.LogEventAsync(
                    userId,
                    "CHECKOUT_SESSION_FAILED",
                    createdFromIP ?? "Unknown",
                    "StripeCheckoutService",
                    false,
                    "Invalid subscription tier");

                return new CheckoutSessionResult
                {
                    Success = false,
                    ErrorMessage = "Invalid subscription tier"
                };
            }

            if (!tier.IsActive)
            {
                await _auditLogService.LogEventAsync(
                    userId,
                    "CHECKOUT_SESSION_FAILED",
                    createdFromIP ?? "Unknown",
                    "StripeCheckoutService",
                    false,
                    "Subscription tier is not active");

                return new CheckoutSessionResult
                {
                    Success = false,
                    ErrorMessage = "Subscription tier is not active"
                };
            }

            // Get or create Stripe customer
            var externalCustomerId = await GetOrCreateStripeCustomerAsync(userId);

            // Determine price based on billing cycle
            var unitAmount = billingCycle == BillingCycle.Annual ? tier.AnnualPrice ?? tier.Price : tier.Price;
            var currency = "usd"; // Default to USD
            var interval = billingCycle == BillingCycle.Annual ? "year" : "month";
            var intervalCount = billingCycle == BillingCycle.Annual ? 1 : 1;

            string sessionId;
            string sessionUrl;

            // Check if Stripe is enabled - if not, return mock data for testing
            if (!_stripeSettings.IsEnabled)
            {
                // Test mode - return mock session data
                sessionId = $"cs_test_{Guid.NewGuid():N}";
                sessionUrl = $"https://checkout.stripe.com/test/{sessionId}";

                _logger.LogInformation(
                    "Created MOCK checkout session {SessionId} for user {UserId} and tier {TierName} with trial {TrialDays}d (Stripe disabled)",
                    sessionId, userId, tier.Name, trialPeriodDays?.ToString() ?? "none");
            }
            else
            {
                // Production mode - make real Stripe API call
                var stripePriceId = await CreateOrUpdatePriceAsync(tier, unitAmount, currency, interval, intervalCount);

                var subscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["user_id"] = userId.ToString(),
                        ["tier_id"] = tierId.ToString(),
                        ["billing_cycle"] = billingCycle.ToString(),
                        ["tier_name"] = tier.Name
                    }
                };

                if (trialPeriodDays.HasValue)
                {
                    subscriptionData.TrialPeriodDays = trialPeriodDays.Value;
                }

                var options = new SessionCreateOptions
                {
                    Customer = externalCustomerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "subscription",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = stripePriceId,
                            Quantity = 1
                        }
                    },
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        ["user_id"] = userId.ToString(),
                        ["tier_id"] = tierId.ToString(),
                        ["billing_cycle"] = billingCycle.ToString(),
                        ["tier_name"] = tier.Name
                    },
                    SubscriptionData = subscriptionData,
                    AllowPromotionCodes = true,
                    TaxIdCollection = new SessionTaxIdCollectionOptions
                    {
                        Enabled = false // Enable for business customers if needed
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);
                sessionId = session.Id;
                sessionUrl = session.Url;

                _logger.LogInformation("Created checkout session {SessionId} for user {UserId} and tier {TierName}",
                    sessionId, userId, tier.Name);
            }

            // Log checkout creation
            await _auditLogService.LogEventAsync(
                userId,
                "CHECKOUT_SESSION_CREATED",
                createdFromIP ?? "Unknown",
                "StripeCheckoutService",
                true,
                $"Created checkout session for {tier.Name} ({billingCycle})",
                sessionId);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = sessionId,
                SessionUrl = sessionUrl,
                CustomerId = externalCustomerId,
                TierId = tierId,
                TierName = tier.Name,
                Amount = unitAmount,
                Currency = currency,
                BillingCycle = billingCycle
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for user {UserId} and tier {TierId}", userId, tierId);

            await _auditLogService.LogEventAsync(
                userId,
                "CHECKOUT_SESSION_FAILED",
                createdFromIP ?? "Unknown",
                "StripeCheckoutService",
                false,
                $"Failed to create checkout session: {ex.Message}");

            return new CheckoutSessionResult
            {
                Success = false,
                ErrorMessage = "Failed to create checkout session"
            };
        }
    }

    /// <summary>
    /// Creates a Stripe Checkout session for adding a payment method
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="successUrl">URL to redirect to on successful setup</param>
    /// <param name="cancelUrl">URL to redirect to on cancelled setup</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Checkout session for payment method setup</returns>
    public async Task<CheckoutSessionResult> CreatePaymentMethodSetupSessionAsync(
        Guid userId,
        string successUrl,
        string cancelUrl,
        string? createdFromIP = null)
    {
        try
        {
            // Get or create Stripe customer
            var externalCustomerId = await GetOrCreateStripeCustomerAsync(userId);

            string sessionId;
            string sessionUrl;

            // Check if Stripe is enabled - if not, return mock data for testing
            if (!_stripeSettings.IsEnabled)
            {
                // Test mode - return mock session data
                sessionId = $"cs_test_{Guid.NewGuid():N}";
                sessionUrl = $"https://checkout.stripe.com/test/{sessionId}";

                _logger.LogInformation("Created MOCK payment method setup session {SessionId} for user {UserId} (Stripe disabled)",
                    sessionId, userId);
            }
            else
            {
                // Production mode - make real Stripe API call
                var options = new SessionCreateOptions
                {
                    Customer = externalCustomerId,
                    Mode = "setup",
                    PaymentMethodTypes = new List<string> { "card" },
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        ["user_id"] = userId.ToString(),
                        ["purpose"] = "payment_method_setup"
                    },
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);
                sessionId = session.Id;
                sessionUrl = session.Url;

                _logger.LogInformation("Created payment method setup session {SessionId} for user {UserId}",
                    sessionId, userId);
            }

            await _auditLogService.LogEventAsync(
                userId,
                "PAYMENT_METHOD_SETUP_SESSION_CREATED",
                createdFromIP ?? "Unknown",
                "StripeCheckoutService",
                true,
                "Created payment method setup session",
                sessionId);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = sessionId,
                SessionUrl = sessionUrl,
                CustomerId = externalCustomerId,
                IsPaymentMethodSetup = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method setup session for user {UserId}", userId);

            await _auditLogService.LogEventAsync(
                userId,
                "PAYMENT_METHOD_SETUP_SESSION_FAILED",
                createdFromIP ?? "Unknown",
                "StripeCheckoutService",
                false,
                $"Failed to create payment method setup session: {ex.Message}");

            return new CheckoutSessionResult
            {
                Success = false,
                ErrorMessage = "Failed to create payment method setup session"
            };
        }
    }

    /// <summary>
    /// Retrieves a checkout session by ID
    /// </summary>
    /// <param name="sessionId">Stripe session ID</param>
    /// <returns>Session details</returns>
    public async Task<CheckoutSessionDetails?> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            // Check if Stripe is enabled - if not, return mock data for testing
            if (!_stripeSettings.IsEnabled)
            {
                // Test mode - validate session ID format
                // Valid format: "cs_test_" + 32 hex characters (GUID without hyphens)
                if (!sessionId.StartsWith("cs_test_") || sessionId.Length != 40)
                {
                    return null; // Invalid session ID format or length
                }

                // Verify last 32 chars are valid hex (GUID format)
                var guidPart = sessionId.Substring(8); // Skip "cs_test_"
                if (guidPart.Length != 32 || !IsValidHex(guidPart))
                {
                    return null;
                }

                return new CheckoutSessionDetails
                {
                    SessionId = sessionId,
                    CustomerId = "cus_test_mock",
                    CustomerEmail = "test@example.com",
                    Status = "complete",
                    PaymentStatus = "paid",
                    AmountTotal = 1999,
                    Currency = "usd",
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    SuccessUrl = "https://example.com/success",
                    CancelUrl = "https://example.com/cancel",
                    Metadata = new Dictionary<string, string>(),
                    SubscriptionId = "sub_test_mock",
                    PaymentIntentId = null
                };
            }

            // Production mode - make real Stripe API call
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, new SessionGetOptions
            {
                Expand = new List<string> { "customer", "subscription", "line_items", "payment_intent" }
            });

            if (session == null)
            {
                return null;
            }

            return new CheckoutSessionDetails
            {
                SessionId = session.Id,
                CustomerId = session.CustomerId,
                CustomerEmail = session.CustomerDetails?.Email,
                Status = session.Status,
                PaymentStatus = session.PaymentStatus,
                AmountTotal = session.AmountTotal,
                Currency = session.Currency,
                Created = ((DateTimeOffset)session.Created).ToUnixTimeSeconds(),
                SuccessUrl = session.SuccessUrl,
                CancelUrl = session.CancelUrl,
                Metadata = session.Metadata,
                SubscriptionId = session.Subscription?.Id,
                PaymentIntentId = session.PaymentIntentId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving checkout session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Creates or retrieves a Stripe price for a subscription tier
    /// </summary>
    private async Task<string> CreateOrUpdatePriceAsync(
        SubscriptionTier tier,
        decimal amount,
        string currency,
        string interval,
        int intervalCount)
    {
        try
        {
            // Check if price already exists for this tier and billing cycle
            var priceService = new PriceService();

            // Use a different approach to find existing prices since Query is not available
            var allPrices = await priceService.ListAsync(new PriceListOptions
            {
                Active = true,
                Limit = 100 // Get more prices to search through
            });

            var existingPrice = allPrices.Data.FirstOrDefault(p =>
                p.Metadata.TryGetValue("tier_id", out var tierId) && tierId == tier.Id.ToString() &&
                p.Metadata.TryGetValue("interval", out var intervalVal) && intervalVal == interval);

            if (existingPrice != null)
            {
                return existingPrice.Id;
            }

            // Create new price
            var options = new PriceCreateOptions
            {
                UnitAmount = (long)(amount * 100), // Convert to cents
                Currency = currency.ToLower(),
                Recurring = new PriceRecurringOptions
                {
                    Interval = interval,
                    IntervalCount = intervalCount,
                    UsageType = "licensed"
                },
                ProductData = new PriceProductDataOptions
                {
                    Name = $"{tier.Name} ({interval})",
                    Metadata = new Dictionary<string, string>
                    {
                        ["tier_id"] = tier.Id.ToString(),
                        ["interval"] = interval,
                        ["features"] = tier.Features != null ? string.Join(", ", tier.Features) : ""
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["tier_id"] = tier.Id.ToString(),
                    ["interval"] = interval,
                    ["tier_name"] = tier.Name
                }
            };

            var price = await priceService.CreateAsync(options);
            return price.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating price for tier {TierId}", tier.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates a Stripe customer for the user
    /// </summary>
    private async Task<string> GetOrCreateStripeCustomerAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("User not found");

            // Check if Stripe is enabled - if not, return mock customer ID
            if (!_stripeSettings.IsEnabled)
            {
                // Test mode - use existing customer ID or create mock ID
                if (!string.IsNullOrEmpty(user.ExternalCustomerId))
                {
                    return user.ExternalCustomerId;
                }

                var mockCustomerId = $"cus_test_{Guid.NewGuid():N}";
                user.ExternalCustomerId = mockCustomerId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created MOCK Stripe customer {CustomerId} for user {UserId} (Stripe disabled)",
                    mockCustomerId, userId);

                return mockCustomerId;
            }

            // Production mode - check if user already has an external customer ID
            if (!string.IsNullOrEmpty(user.ExternalCustomerId))
            {
                return user.ExternalCustomerId;
            }

            // Search for existing customer by email
            var customerService = new CustomerService();
            var existingCustomers = await customerService.ListAsync(new CustomerListOptions
            {
                Email = user.Email,
                Limit = 1
            });

            if (existingCustomers.Any())
            {
                var customerId = existingCustomers.First().Id;
                user.ExternalCustomerId = customerId;
                await _context.SaveChangesAsync();
                return customerId;
            }

            // Create new customer
            var options = new CustomerCreateOptions
            {
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["created_from"] = "skillledger_subscription"
                },
                Address = new AddressOptions
                {
                    Country = "US", // Default, can be updated from user profile
                    Line1 = "Address",
                    City = "City",
                    State = "State",
                    PostalCode = "00000"
                }
            };

            var customer = await customerService.CreateAsync(options);

            // Save customer ID to user record
            user.ExternalCustomerId = customer.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);
            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating Stripe customer for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Validates if a string contains only hexadecimal characters
    /// </summary>
    private static bool IsValidHex(string value)
    {
        foreach (char c in value)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// Result of creating a checkout session
/// </summary>
public class CheckoutSessionResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? SessionUrl { get; set; }
    public string? CustomerId { get; set; }
    public Guid? TierId { get; set; }
    public string? TierName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public BillingCycle BillingCycle { get; set; }
    public bool IsPaymentMethodSetup { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Details of a checkout session
/// </summary>
public class CheckoutSessionDetails
{
    public string SessionId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public long? AmountTotal { get; set; }
    public string? Currency { get; set; }
    public long Created { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public string? SubscriptionId { get; set; }
    public string? PaymentIntentId { get; set; }
}

/// <summary>
/// Billing cycle options
/// </summary>
public enum BillingCycle
{
    Monthly,
    Annual
}