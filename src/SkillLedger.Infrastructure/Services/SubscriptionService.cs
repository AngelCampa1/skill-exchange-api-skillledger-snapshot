using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private const int TrialDurationDays = 30;

    private readonly SkillLedgerDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ICreditWalletService _creditWalletService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        SkillLedgerDbContext context,
        IPaymentService paymentService,
        ICreditWalletService creditWalletService,
        IAuditLogService auditLogService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _creditWalletService = creditWalletService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        Guid paymentMethodId,
        bool isTrial = false,
        bool isAnnual = false,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Creating subscription for user {UserId}, tier {TierId}, trial: {IsTrial}",
            userId, subscriptionTierId, isTrial);

        try
        {
            // Validate user exists and doesn't have active subscription
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            var existingActiveSubscription = await GetUserActiveSubscriptionAsync(userId);
            if (existingActiveSubscription != null)
                throw new InvalidOperationException("User already has an active subscription");

            // Get subscription tier
            var tier = await _context.SubscriptionTiers.FindAsync(subscriptionTierId);
            if (tier == null || !tier.IsActive)
                throw new ArgumentException("Invalid or inactive subscription tier", nameof(subscriptionTierId));

            // Validate payment method
            var paymentMethod = await _paymentService.GetPaymentMethodAsync(paymentMethodId, userId);
            if (paymentMethod == null || !paymentMethod.IsValid)
                throw new ArgumentException("Invalid or inactive payment method", nameof(paymentMethodId));

            // Create external customer if doesn't exist
            if (string.IsNullOrEmpty(user.ExternalCustomerId))
            {
                var externalCustomerId = await _paymentService.CreateExternalCustomerAsync(
                    userId, user.Email!, $"{user.FirstName} {user.LastName}".Trim());
                user.ExternalCustomerId = externalCustomerId;
                await _context.SaveChangesAsync();
            }

            // Calculate dates
            var now = DateTime.UtcNow;
            var startDate = now;
            DateTime? trialEndDate = null;
            DateTime? nextBillingDate = null;
            var status = SubscriptionStatus.Active;
            decimal amount = 0;

            if (isTrial)
            {
                trialEndDate = now.AddDays(TrialDurationDays); // 30-day trial
                nextBillingDate = trialEndDate.Value;
                status = SubscriptionStatus.Trial;
                amount = 0;
            }
            else
            {
                nextBillingDate = isAnnual ? now.AddYears(1) : now.AddMonths(1);
                amount = isAnnual && tier.AnnualPrice.HasValue ? tier.AnnualPrice.Value : tier.Price;
            }

            // Create subscription
            var subscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionTierId = subscriptionTierId,
                Status = status,
                StartDate = startDate,
                TrialEndDate = trialEndDate,
                NextBillingDate = nextBillingDate,
                PaymentMethodId = paymentMethodId,
                IsAnnual = isAnnual,
                BillingCycleCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Process payment if not trial
            if (!isTrial && amount > 0)
            {
                var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(
                    subscription.Id, amount, "USD", $"Subscription - {tier.Name}", createdFromIP);

                if (!paymentResult.Success)
                {
                    subscription.Status = SubscriptionStatus.PastDue;
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException($"Payment failed: {paymentResult.ErrorMessage}");
                }

                // Create transaction record
                await CreateSubscriptionTransactionAsync(
                    subscription.Id,
                    userId,
                    SubscriptionTransactionType.Purchase,
                    amount,
                    paymentMethodId,
                    paymentResult.ExternalTransactionId,
                    TransactionStatus.Completed,
                    createdFromIP);
            }

            // Award bonus credits if applicable
            if (tier.CreditBonus > 0)
            {
                await _creditWalletService.TransferCreditsAsync(
                    Guid.Empty, // System account
                    userId,
                    tier.CreditBonus,
                    $"Welcome bonus - {tier.Name}",
                    CreditTransactionType.BonusPayment,
                    null,
                    createdFromIP);
            }

            // BUG-NEW-003 FIX: Use null-coalescing for nullable ipAddress
            // Log audit event
            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_CREATED",
                createdFromIP ?? "Unknown",
                null,
                true,
                $"Subscription created: {tier.Name}, Trial: {isTrial}, Annual: {isAnnual}");

            _logger.LogInformation("Successfully created subscription {SubscriptionId} for user {UserId}",
                subscription.Id, userId);

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_CREATE_FAILED",
                createdFromIP ?? "Unknown",
                null,
                false,
                $"Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a subscription from Stripe Checkout session (webhook handler)
    /// This is called when checkout.session.completed webhook is received
    /// </summary>
    public Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        string? stripeSubscriptionId,
        string? stripeCustomerId)
    {
        // Delegate to the overload with null promotion info
        return CreateSubscriptionAsync(userId, subscriptionTierId, stripeSubscriptionId, stripeCustomerId, null);
    }

    /// <summary>
    /// Creates a subscription from Stripe Checkout session with promotion info (webhook handler)
    /// This is called when checkout.session.completed webhook is received
    /// </summary>
    public async Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        string? stripeSubscriptionId,
        string? stripeCustomerId,
        SubscriptionPromotionInfo? promotionInfo)
    {
        _logger.LogInformation(
            "Creating subscription from Stripe Checkout for user {UserId}, tier {TierId}, Stripe sub: {StripeSubId}, Promo: {PromoCode}",
            userId, subscriptionTierId, stripeSubscriptionId, promotionInfo?.PromoCode ?? "none");

        try
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            // Check for existing active subscription
            var existingActiveSubscription = await GetUserActiveSubscriptionAsync(userId);
            if (existingActiveSubscription != null)
            {
                // If already has subscription with same tier, just return it (idempotent)
                if (existingActiveSubscription.SubscriptionTierId == subscriptionTierId)
                {
                    _logger.LogInformation("User {UserId} already has active subscription to tier {TierId}", userId, subscriptionTierId);
                    return existingActiveSubscription;
                }
                throw new InvalidOperationException("User already has an active subscription to a different tier");
            }

            // Check if subscription already exists by Stripe ID (idempotent handling)
            if (!string.IsNullOrEmpty(stripeSubscriptionId))
            {
                var existingStripeSubscription = await GetSubscriptionByExternalIdAsync(stripeSubscriptionId);
                if (existingStripeSubscription != null)
                {
                    _logger.LogInformation("Subscription already exists for Stripe ID {StripeSubId}", stripeSubscriptionId);
                    return existingStripeSubscription;
                }
            }

            // Get subscription tier
            var tier = await _context.SubscriptionTiers.FindAsync(subscriptionTierId);
            if (tier == null || !tier.IsActive)
                throw new ArgumentException("Invalid or inactive subscription tier", nameof(subscriptionTierId));

            // Update user's Stripe customer ID if provided
            if (!string.IsNullOrEmpty(stripeCustomerId) && user.ExternalCustomerId != stripeCustomerId)
            {
                user.ExternalCustomerId = stripeCustomerId;
            }

            // Create the subscription
            var now = DateTime.UtcNow;
            var subscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionTierId = subscriptionTierId,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                NextBillingDate = now.AddMonths(1), // Default to monthly, Stripe manages actual billing
                ExternalSubscriptionId = stripeSubscriptionId,
                IsAnnual = false, // Will be determined by Stripe price interval
                BillingCycleCount = 1,
                AutoRenew = true,
                CreatedAt = now,
                UpdatedAt = now,
                // Store promotion info if provided
                AppliedCouponId = promotionInfo?.CouponId,
                AppliedPromoCode = promotionInfo?.PromoCode,
                DiscountEndsAt = promotionInfo?.DiscountEndsAt
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Award bonus credits if applicable
            if (tier.CreditBonus > 0)
            {
                await _creditWalletService.TransferCreditsAsync(
                    Guid.Empty, // System account
                    userId,
                    tier.CreditBonus,
                    $"Welcome bonus - {tier.Name}",
                    CreditTransactionType.BonusPayment,
                    null,
                    null);
            }

            // Build promotion details for audit log
            var promoDetails = promotionInfo != null
                ? $", Promo: {promotionInfo.PromoCode ?? promotionInfo.CouponId}, Discount ends: {promotionInfo.DiscountEndsAt?.ToString("yyyy-MM-dd") ?? "never"}"
                : "";

            // Log audit event
            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_CREATED_VIA_STRIPE",
                "Webhook",
                null,
                true,
                $"Subscription created via Stripe Checkout: {tier.Name}, Stripe ID: {stripeSubscriptionId}{promoDetails}");

            _logger.LogInformation(
                "Successfully created subscription {SubscriptionId} for user {UserId} via Stripe Checkout",
                subscription.Id, userId);

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription from Stripe Checkout for user {UserId}", userId);
            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_CREATE_VIA_STRIPE_FAILED",
                "Webhook",
                null,
                false,
                $"Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Records a successful payment for a subscription (called from webhook)
    /// </summary>
    public async Task RecordPaymentAsync(string stripeSubscriptionId, long amountPaid)
    {
        _logger.LogInformation("Recording payment for Stripe subscription {StripeSubId}, amount: {Amount}",
            stripeSubscriptionId, amountPaid);

        try
        {
            var subscription = await GetSubscriptionByExternalIdAsync(stripeSubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("Subscription not found for Stripe ID {StripeSubId}", stripeSubscriptionId);
                return; // Don't throw - webhook may arrive before subscription is created
            }

            // Update subscription dates
            subscription.LastPaymentDate = DateTime.UtcNow;
            subscription.BillingCycleCount++;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Ensure subscription is active
            if (subscription.Status == SubscriptionStatus.PastDue)
            {
                subscription.Status = SubscriptionStatus.Active;
                _logger.LogInformation("Subscription {SubId} reactivated after payment", subscription.Id);
            }

            _context.SubscriptionTransactions.Add(new SubscriptionTransaction
            {
                SubscriptionId = subscription.Id,
                UserId = subscription.UserId,
                Type = SubscriptionTransactionType.Renewal,
                Amount = amountPaid / 100m,
                Currency = "USD",
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CreatedFromIP = "Webhook"
            });

            await _context.SaveChangesAsync();

            try
            {
                await _auditLogService.LogEventAsync(
                    subscription.UserId,
                    "PAYMENT_RECORDED_VIA_STRIPE",
                    "Webhook",
                    null,
                    true,
                    $"Payment recorded: ${amountPaid / 100m:F2}");
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "Failed to write non-critical payment audit log for subscription {SubId}", subscription.Id);
            }

            _logger.LogInformation("Successfully recorded payment for subscription {SubId}", subscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment for Stripe subscription {StripeSubId}", stripeSubscriptionId);
            throw;
        }
    }

    public async Task<UserSubscription?> GetUserActiveSubscriptionAsync(Guid userId)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        return await _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Include(us => us.PaymentMethod)
            .AsSplitQuery()
            .Where(us => us.UserId == userId &&
                        (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial))
            .OrderByDescending(us => us.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<UserSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        return await _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Include(us => us.PaymentMethod)
            .AsSplitQuery()
            .Where(us => us.ExternalSubscriptionId == externalSubscriptionId)
            .FirstOrDefaultAsync();
    }

    public async Task<(List<UserSubscription> subscriptions, int totalCount)> GetUserSubscriptionsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        var query = _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Include(us => us.PaymentMethod)
            .Include(us => us.Transactions)
            .AsSplitQuery()
            .Where(us => us.UserId == userId)
            .OrderByDescending(us => us.CreatedAt);

        var totalCount = await query.CountAsync();
        var subscriptions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (subscriptions, totalCount);
    }

    public async Task<List<SubscriptionTier>> GetSubscriptionTiersAsync()
    {
        return await _context.SubscriptionTiers
            .Where(st => st.IsActive)
            .OrderBy(st => st.SortOrder)
            .ToListAsync();
    }

    public async Task<SubscriptionTier?> GetSubscriptionTierAsync(Guid tierId)
    {
        return await _context.SubscriptionTiers
            .Where(st => st.Id == tierId && st.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<UserSubscription> UpgradeSubscriptionAsync(
        Guid userId,
        Guid newTierId,
        bool immediateCharge = true,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Upgrading subscription for user {UserId} to tier {TierId}", userId, newTierId);

        var currentSubscription = await GetUserActiveSubscriptionAsync(userId);
        if (currentSubscription == null)
            throw new InvalidOperationException("No active subscription found");

        var newTier = await GetSubscriptionTierAsync(newTierId);
        if (newTier == null)
            throw new ArgumentException("Invalid subscription tier", nameof(newTierId));

        if (newTier.Price <= currentSubscription.SubscriptionTier.Price)
            throw new InvalidOperationException("New tier must be more expensive than current tier");

        try
        {
            // Process payment if immediate charge
            if (immediateCharge && currentSubscription.Status != SubscriptionStatus.Trial)
            {
                var proratedAmount = CalculateProratedAmount(currentSubscription, newTier);
                if (proratedAmount > 0)
                {
                    var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(
                        currentSubscription.Id, proratedAmount, "USD",
                        $"Subscription upgrade to {newTier.Name}", createdFromIP);

                    if (!paymentResult.Success)
                        throw new InvalidOperationException($"Upgrade payment failed: {paymentResult.ErrorMessage}");

                    // Record upgrade transaction
                    await CreateSubscriptionTransactionAsync(
                        currentSubscription.Id,
                        userId,
                        SubscriptionTransactionType.Upgrade,
                        proratedAmount,
                        currentSubscription.PaymentMethodId,
                        paymentResult.ExternalTransactionId,
                        TransactionStatus.Completed,
                        createdFromIP);
                }
            }

            // Update subscription
            currentSubscription.SubscriptionTierId = newTierId;
            currentSubscription.UpdatedAt = DateTime.UtcNow;

            // Award additional credits if applicable
            var creditDifference = newTier.CreditBonus - currentSubscription.SubscriptionTier.CreditBonus;
            if (creditDifference > 0)
            {
                await _creditWalletService.TransferCreditsAsync(
                    Guid.Empty, userId, creditDifference,
                    $"Upgrade bonus - {newTier.Name}",
                    CreditTransactionType.BonusPayment,
                    null,
                    createdFromIP);
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_UPGRADED",
                createdFromIP ?? "Unknown",
                null,
                true,
                $"Upgraded from {currentSubscription.SubscriptionTier.Name} to {newTier.Name}");

            return currentSubscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSubscription> DowngradeSubscriptionAsync(
        Guid userId,
        Guid newTierId,
        DateTime? effectiveDate = null,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Downgrading subscription for user {UserId} to tier {TierId}", userId, newTierId);

        var currentSubscription = await GetUserActiveSubscriptionAsync(userId);
        if (currentSubscription == null)
            throw new InvalidOperationException("No active subscription found");

        var newTier = await GetSubscriptionTierAsync(newTierId);
        if (newTier == null)
            throw new ArgumentException("Invalid subscription tier", nameof(newTierId));

        if (newTier.Price >= currentSubscription.SubscriptionTier.Price)
            throw new InvalidOperationException("New tier must be less expensive than current tier");

        try
        {
            // Schedule downgrade for next billing date if not immediate
            if (!effectiveDate.HasValue)
                effectiveDate = currentSubscription.NextBillingDate;

            // Update subscription with downgrade schedule
            currentSubscription.EndDate = effectiveDate;
            currentSubscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_DOWNGRADE_SCHEDULED",
                createdFromIP ?? "Unknown",
                null,
                true,
                $"Downgrade from {currentSubscription.SubscriptionTier.Name} to {newTier.Name} effective {effectiveDate:d}");

            return currentSubscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading subscription for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSubscription> CancelSubscriptionAsync(
        Guid userId,
        string? reason = null,
        bool immediate = false,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Cancelling subscription for user {UserId}, immediate: {Immediate}", userId, immediate);

        var subscription = await GetUserActiveSubscriptionAsync(userId);
        if (subscription == null)
            throw new InvalidOperationException("No active subscription found");

        try
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancellationReason = reason;
            subscription.AutoRenew = false;

            if (immediate)
            {
                subscription.EndDate = DateTime.UtcNow;
            }
            else
            {
                // Keep active until end of billing period
                subscription.EndDate = subscription.NextBillingDate;
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "SUBSCRIPTION_CANCELLED",
                createdFromIP ?? "Unknown",
                null,
                true,
                $"Cancelled, immediate: {immediate}, reason: {reason}");

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSubscription> RenewSubscriptionAsync(Guid subscriptionId, string? createdFromIP = null)
    {
        _logger.LogInformation("Renewing subscription {SubscriptionId}", subscriptionId);

        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        var subscription = await _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Include(us => us.PaymentMethod)
            .AsSplitQuery()
            .FirstOrDefaultAsync(us => us.Id == subscriptionId);

        if (subscription == null)
            throw new ArgumentException("Subscription not found", nameof(subscriptionId));

        if (subscription.Status == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("Cannot renew cancelled subscription");

        try
        {
            var amount = subscription.IsAnnual && subscription.SubscriptionTier.AnnualPrice.HasValue
                ? subscription.SubscriptionTier.AnnualPrice.Value
                : subscription.SubscriptionTier.Price;

            // Process renewal payment
            var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(
                subscription.Id, amount, "USD",
                $"Subscription renewal - {subscription.SubscriptionTier.Name}", createdFromIP);

            if (!paymentResult.Success)
            {
                subscription.Status = SubscriptionStatus.PastDue;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException($"Renewal payment failed: {paymentResult.ErrorMessage}");
            }

            // Update subscription
            subscription.Status = SubscriptionStatus.Active;
            // BUG-NEW-005 FIX: Handle nullable NextBillingDate safely
            if (!subscription.NextBillingDate.HasValue)
            {
                throw new InvalidOperationException("Cannot renew subscription without NextBillingDate set");
            }
            subscription.NextBillingDate = subscription.IsAnnual
                ? subscription.NextBillingDate.Value.AddYears(1)
                : subscription.NextBillingDate.Value.AddMonths(1);
            subscription.BillingCycleCount++;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Record renewal transaction
            await CreateSubscriptionTransactionAsync(
                subscription.Id,
                subscription.UserId,
                SubscriptionTransactionType.Renewal,
                amount,
                subscription.PaymentMethodId,
                paymentResult.ExternalTransactionId,
                TransactionStatus.Completed,
                createdFromIP);

            // Award monthly credits
            if (subscription.SubscriptionTier.CreditBonus > 0)
            {
                await _creditWalletService.TransferCreditsAsync(
                    Guid.Empty, subscription.UserId, subscription.SubscriptionTier.CreditBonus,
                    $"Monthly bonus - {subscription.SubscriptionTier.Name}",
                    CreditTransactionType.BonusPayment,
                    null,
                    createdFromIP);
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                subscription.UserId,
                "SUBSCRIPTION_RENEWED",
                createdFromIP ?? "Unknown",
                null,
                true,
                $"Renewed {subscription.SubscriptionTier.Name}, cycle: {subscription.BillingCycleCount}");

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<UserSubscription> PauseSubscriptionAsync(
        Guid userId,
        TimeSpan pauseDuration,
        string? createdFromIP = null)
    {
        var subscription = await GetUserActiveSubscriptionAsync(userId);
        if (subscription == null)
            throw new InvalidOperationException("No active subscription found");

        subscription.Status = SubscriptionStatus.Suspended;
        subscription.EndDate = DateTime.UtcNow.Add(pauseDuration);
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(
            userId,
            "SUBSCRIPTION_PAUSED",
            createdFromIP ?? "Unknown",
            null,
            true,
            $"Paused for {pauseDuration.TotalDays:F0} days");

        return subscription;
    }

    public async Task<UserSubscription> ResumeSubscriptionAsync(Guid userId, string? createdFromIP = null)
    {
        var subscription = await _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Where(us => us.UserId == userId && us.Status == SubscriptionStatus.Suspended)
            .FirstOrDefaultAsync();

        if (subscription == null)
            throw new InvalidOperationException("No paused subscription found");

        subscription.Status = SubscriptionStatus.Active;
        subscription.EndDate = null;
        subscription.NextBillingDate = DateTime.UtcNow.AddMonths(1);
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(
            userId,
            "SUBSCRIPTION_RESUMED",
            createdFromIP ?? "Unknown",
            null,
            true,
            $"Resumed {subscription.SubscriptionTier.Name}");

        return subscription;
    }

    public async Task<bool> HasFeatureAccessAsync(Guid userId, string feature)
    {
        var subscription = await GetUserActiveSubscriptionAsync(userId);
        if (subscription == null)
            return false; // No subscription = no premium features

        var tier = subscription.SubscriptionTier;

        return feature.ToLower() switch
        {
            "prioritysupport" => tier.PrioritySupport,
            "apiaccess" => tier.ApiAccess,
            "advancedanalytics" => tier.AdvancedAnalytics,
            "advancedfrauddetection" => tier.AdvancedFraudDetection,
            "multisignature" => tier.MultiSignature,
            "customintegrations" => tier.CustomIntegrations,
            _ => tier.Features?.Contains(feature, StringComparison.OrdinalIgnoreCase) ?? false
        };
    }

    public async Task<SubscriptionLimitsDto> GetUserSubscriptionLimitsAsync(Guid userId)
    {
        var subscription = await GetUserActiveSubscriptionAsync(userId);
        if (subscription == null)
        {
            // Return default free tier limits
            return new SubscriptionLimitsDto
            {
                MaxActiveProjects = 1,
                MaxTeamMembers = 0,
                MaxMonthlyEarnings = 500,
                PrioritySupport = false,
                ApiAccess = false,
                AdvancedAnalytics = false,
                AdvancedFraudDetection = false,
                MultiSignature = false,
                CustomIntegrations = false,
                Features = new List<string>()
            };
        }

        var tier = subscription.SubscriptionTier;
        var features = new List<string>();

        if (tier.PrioritySupport) features.Add("PrioritySupport");
        if (tier.ApiAccess) features.Add("ApiAccess");
        if (tier.AdvancedAnalytics) features.Add("AdvancedAnalytics");
        if (tier.AdvancedFraudDetection) features.Add("AdvancedFraudDetection");
        if (tier.MultiSignature) features.Add("MultiSignature");
        if (tier.CustomIntegrations) features.Add("CustomIntegrations");

        if (!string.IsNullOrEmpty(tier.Features))
        {
            try
            {
                var additionalFeatures = JsonSerializer.Deserialize<List<string>>(tier.Features);
                if (additionalFeatures != null)
                    features.AddRange(additionalFeatures);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to parse features JSON for tier {TierId}", tier.Id);
            }
        }

        return new SubscriptionLimitsDto
        {
            MaxActiveProjects = tier.MaxActiveProjects,
            MaxTeamMembers = tier.MaxTeamMembers,
            MaxMonthlyEarnings = tier.MaxMonthlyEarnings,
            PrioritySupport = tier.PrioritySupport,
            ApiAccess = tier.ApiAccess,
            AdvancedAnalytics = tier.AdvancedAnalytics,
            AdvancedFraudDetection = tier.AdvancedFraudDetection,
            MultiSignature = tier.MultiSignature,
            CustomIntegrations = tier.CustomIntegrations,
            Features = features
        };
    }

    public async Task<UserSubscription> ConvertTrialToPaidAsync(
        Guid userId,
        Guid paymentMethodId,
        string? createdFromIP = null)
    {
        var trialSubscription = await _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Where(us => us.UserId == userId && us.Status == SubscriptionStatus.Trial)
            .FirstOrDefaultAsync();

        if (trialSubscription == null)
            throw new InvalidOperationException("No trial subscription found");

        // Update payment method
        trialSubscription.PaymentMethodId = paymentMethodId;
        trialSubscription.Status = SubscriptionStatus.Active;
        trialSubscription.TrialEndDate = null;
        trialSubscription.UpdatedAt = DateTime.UtcNow;

        // Process first payment
        var amount = trialSubscription.IsAnnual && trialSubscription.SubscriptionTier.AnnualPrice.HasValue
            ? trialSubscription.SubscriptionTier.AnnualPrice.Value
            : trialSubscription.SubscriptionTier.Price;

        var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(
            trialSubscription.Id, amount, "USD",
            $"Trial conversion - {trialSubscription.SubscriptionTier.Name}", createdFromIP);

        if (!paymentResult.Success)
        {
            trialSubscription.Status = SubscriptionStatus.PastDue;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException($"Trial conversion payment failed: {paymentResult.ErrorMessage}");
        }

        // Record conversion transaction
        await CreateSubscriptionTransactionAsync(
            trialSubscription.Id,
            userId,
            SubscriptionTransactionType.TrialConversion,
            amount,
            paymentMethodId,
            paymentResult.ExternalTransactionId,
            TransactionStatus.Completed,
            createdFromIP);

        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(
            userId,
            "TRIAL_CONVERTED",
            createdFromIP ?? "Unknown",
            null,
            true,
            $"Converted trial to {trialSubscription.SubscriptionTier.Name}");

        return trialSubscription;
    }

    public async Task<SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var subscriptions = await _context.UserSubscriptions
            .Include(us => us.SubscriptionTier)
            .Where(us => us.CreatedAt >= startDate && us.CreatedAt <= endDate)
            .ToListAsync();

        var activeSubscriptions = await _context.UserSubscriptions
            .Where(us => us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial)
            .ToListAsync();

        var statistics = new SubscriptionStatisticsDto
        {
            TotalSubscriptions = subscriptions.Count,
            ActiveSubscriptions = activeSubscriptions.Count,
            TrialSubscriptions = subscriptions.Count(us => us.Status == SubscriptionStatus.Trial),
            CancelledSubscriptions = subscriptions.Count(us => us.Status == SubscriptionStatus.Cancelled),
            ExpiredSubscriptions = subscriptions.Count(us => us.Status == SubscriptionStatus.Expired),
            NewSubscriptionsThisPeriod = subscriptions.Count,
            ChurnedSubscriptionsThisPeriod = subscriptions.Count(us => us.Status == SubscriptionStatus.Cancelled),
            SubscriptionsByTier = new Dictionary<string, int>(),
            SubscriptionsByStatus = new Dictionary<string, int>()
        };

        // Calculate MRR and ARR
        foreach (var active in activeSubscriptions)
        {
            var monthlyAmount = active.IsAnnual && active.SubscriptionTier.AnnualPrice.HasValue
                ? active.SubscriptionTier.AnnualPrice.Value / 12
                : active.SubscriptionTier.Price;

            statistics.MonthlyRecurringRevenue += monthlyAmount;
        }
        statistics.AnnualRecurringRevenue = statistics.MonthlyRecurringRevenue * 12;

        // Group by tier
        statistics.SubscriptionsByTier = activeSubscriptions
            .GroupBy(us => us.SubscriptionTier.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by status
        statistics.SubscriptionsByStatus = Enum.GetValues<SubscriptionStatus>()
            .ToDictionary(
                status => status.ToString(),
                status => subscriptions.Count(us => us.Status == status));

        return statistics;
    }

    private async Task<SubscriptionTransaction> CreateSubscriptionTransactionAsync(
        Guid subscriptionId,
        Guid userId,
        SubscriptionTransactionType type,
        decimal amount,
        Guid? paymentMethodId,
        string? externalTransactionId,
        TransactionStatus status,
        string? createdFromIP)
    {
        var transaction = new SubscriptionTransaction
        {
            SubscriptionId = subscriptionId,
            UserId = userId,
            Type = type,
            Amount = amount,
            Currency = "USD",
            PaymentMethodId = paymentMethodId,
            ExternalTransactionId = externalTransactionId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = status == TransactionStatus.Completed ? DateTime.UtcNow : null,
            CompletedAt = status == TransactionStatus.Completed ? DateTime.UtcNow : null,
            CreatedFromIP = createdFromIP
        };

        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction;
    }

    private decimal CalculateProratedAmount(UserSubscription currentSubscription, SubscriptionTier newTier)
    {
        var currentPrice = currentSubscription.IsAnnual && currentSubscription.SubscriptionTier.AnnualPrice.HasValue
            ? currentSubscription.SubscriptionTier.AnnualPrice.Value
            : currentSubscription.SubscriptionTier.Price;

        var newPrice = currentSubscription.IsAnnual && newTier.AnnualPrice.HasValue
            ? newTier.AnnualPrice.Value
            : newTier.Price;

        var priceDifference = newPrice - currentPrice;
        if (priceDifference <= 0)
            return 0;

        // Calculate remaining time in current billing period
        var remainingDays = (currentSubscription.NextBillingDate!.Value - DateTime.UtcNow).TotalDays;
        var totalDays = (currentSubscription.NextBillingDate!.Value - currentSubscription.StartDate).TotalDays;

        return priceDifference * (decimal)(remainingDays / totalDays);
    }

    public async Task<UserUsageStatisticsDto> GetUserUsageStatisticsAsync(Guid userId)
    {
        _logger.LogInformation("Getting usage statistics for user {UserId}", userId);

        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Count active projects owned by the user
            // Active projects are those in Draft, Published, InProgress, or Active status
            var activeProjectStatuses = new[]
            {
                ProjectStatus.Draft,
                ProjectStatus.Published,
                ProjectStatus.InProgress
            };

            var activeProjectsCount = await _context.Projects
                .Where(p => p.ClientId == userId && activeProjectStatuses.Contains(p.Status))
                .CountAsync();

            // Count total projects created
            var totalProjectsCount = await _context.Projects
                .Where(p => p.ClientId == userId)
                .CountAsync();

            // Count team members (providers working on user's projects)
            var teamMembersCount = await _context.ProjectApplications
                .Where(a => a.Project.ClientId == userId &&
                           a.Status == ApplicationStatus.Accepted)
                .Select(a => a.ProviderId)
                .Distinct()
                .CountAsync();

            // Earnings transaction types (credits received for work)
            var earningTypes = new[]
            {
                CreditTransactionType.ProjectPayment,
                CreditTransactionType.DirectPayment,
                CreditTransactionType.EscrowRelease,
                CreditTransactionType.BonusPayment
            };

            // Calculate monthly earnings (credits received this month)
            var monthlyEarnings = await _context.CreditTransactions
                .Where(t => t.ToUserId == userId &&
                           t.CreatedAt >= startOfMonth &&
                           t.Status == TransactionStatus.Completed &&
                           earningTypes.Contains(t.Type))
                .SumAsync(t => (int?)t.Amount) ?? 0;

            // Spending transaction types (credits paid out)
            var spendingTypes = new[]
            {
                CreditTransactionType.ProjectPayment,
                CreditTransactionType.DirectPayment,
                CreditTransactionType.EscrowDeposit
            };

            // Calculate monthly spending (credits spent this month)
            var monthlySpending = await _context.CreditTransactions
                .Where(t => t.FromUserId == userId &&
                           t.CreatedAt >= startOfMonth &&
                           t.Status == TransactionStatus.Completed &&
                           spendingTypes.Contains(t.Type))
                .SumAsync(t => (int?)t.Amount) ?? 0;

            // Count total applications (sent and received)
            var totalApplications = await _context.ProjectApplications
                .Where(a => a.ProviderId == userId || a.Project.ClientId == userId)
                .CountAsync();

            return new UserUsageStatisticsDto
            {
                CurrentActiveProjects = activeProjectsCount,
                CurrentTeamMembers = teamMembersCount,
                CurrentMonthlyEarnings = monthlyEarnings,
                CurrentMonthlySpending = monthlySpending,
                TotalProjectsCreated = totalProjectsCount,
                TotalApplications = totalApplications
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics for user {UserId}", userId);
            // Return zero values on error rather than throwing
            return new UserUsageStatisticsDto();
        }
    }
}
