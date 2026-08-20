using System.Collections.Concurrent;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// BUG-CRIT-004 FIX: Service for handling Stripe webhook events with proper signature validation
/// </summary>
public class StripeWebhookService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ChargeRefundLocks = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookService> _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentService _paymentService;
    private readonly IAuditLogService _auditLogService;
    private readonly SkillLedgerDbContext _context;
    private readonly StripeSettings _stripeSettings;
    private readonly string? _webhookSecret;

    public StripeWebhookService(
        IConfiguration configuration,
        ILogger<StripeWebhookService> logger,
        ISubscriptionService subscriptionService,
        IPaymentService paymentService,
        IAuditLogService auditLogService,
        SkillLedgerDbContext context,
        IOptions<StripeSettings> stripeSettings)
    {
        _configuration = configuration;
        _logger = logger;
        _subscriptionService = subscriptionService;
        _paymentService = paymentService;
        _auditLogService = auditLogService;
        _context = context;
        _stripeSettings = stripeSettings.Value;
        _webhookSecret = _configuration["Stripe:WebhookSecret"];

        // Log warning if webhook secret is not configured
        if (string.IsNullOrEmpty(_webhookSecret) || _webhookSecret.StartsWith("REPLACE_WITH"))
        {
            _logger.LogWarning("Stripe webhook secret is not configured. Webhook signature validation will fail.");
        }
    }

    /// <summary>
    /// BUG-CRIT-004 FIX: Validates webhook signature and constructs Stripe event
    /// This prevents attackers from sending fake webhook payloads
    /// </summary>
    /// <param name="json">Raw JSON payload from webhook</param>
    /// <param name="stripeSignatureHeader">Stripe-Signature header value</param>
    /// <returns>Validated Stripe event</returns>
    /// <exception cref="StripeException">Thrown when signature validation fails</exception>
    public Event ConstructEvent(string json, string stripeSignatureHeader)
    {
        if (string.IsNullOrEmpty(_webhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is not configured");
        }

        try
        {
            // BUG-CRIT-004 FIX: Use Stripe's built-in signature validation
            // This verifies the webhook came from Stripe and hasn't been tampered with
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignatureHeader,
                _webhookSecret,
                throwOnApiVersionMismatch: false // Allow minor version differences
            );

            _logger.LogInformation("Successfully validated webhook signature for event type: {EventType}", stripeEvent.Type);
            return stripeEvent;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Webhook signature validation failed");
            throw;
        }
    }

    /// <summary>
    /// Processes a validated Stripe webhook event
    /// </summary>
    /// <param name="stripeEvent">The validated Stripe event</param>
    public async Task ProcessWebhookEventAsync(Event stripeEvent)
    {
        _logger.LogInformation("Processing webhook event: {EventType} (ID: {EventId})",
            stripeEvent.Type, stripeEvent.Id);

        var eventClaimed = false;

        try
        {
            if (!string.IsNullOrWhiteSpace(stripeEvent.Id))
            {
                eventClaimed = await TryClaimWebhookEventAsync(stripeEvent.Id, stripeEvent.Type);
                if (!eventClaimed)
                {
                    _logger.LogInformation("Skipping already claimed or processed Stripe webhook event {EventId}", stripeEvent.Id);
                    return;
                }
            }

            try
            {
            // Handle different event types
            switch (stripeEvent.Type)
            {
                // CRITICAL: This event fires when a customer completes checkout and payment is successful
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;

                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceededAsync(stripeEvent);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailedAsync(stripeEvent);
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    await HandleSubscriptionEventAsync(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(stripeEvent);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaidAsync(stripeEvent);
                    break;

                case "charge.refunded":
                    await HandleChargeRefundedAsync(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(stripeEvent.Id))
            {
                await MarkWebhookEventProcessedAsync(stripeEvent.Id, stripeEvent.Type);
            }
            }
            catch (Exception ex) when (eventClaimed && !string.IsNullOrWhiteSpace(stripeEvent.Id))
            {
                await MarkWebhookEventFailedAsync(stripeEvent.Id, stripeEvent.Type, ex);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType} (ID: {EventId})",
                stripeEvent.Type, stripeEvent.Id);
            throw;
        }
    }

    private async Task<bool> TryClaimWebhookEventAsync(string eventId, string eventType)
    {
        var now = DateTime.UtcNow;
        var staleProcessingCutoff = now.AddMinutes(-10);

        if (_context.Database.IsRelational())
        {
            var updatedRows = await _context.ProcessedStripeWebhookEvents
                .Where(e => e.EventId == eventId &&
                            e.ProcessedAt == null &&
                            (!e.ProcessingStartedAt.HasValue || e.ProcessingStartedAt <= staleProcessingCutoff))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.EventType, eventType)
                    .SetProperty(e => e.ProcessingStartedAt, now)
                    .SetProperty(e => e.ErrorMessage, (string?)null));

            if (updatedRows == 1)
            {
                return true;
            }

            var alreadyExists = await _context.ProcessedStripeWebhookEvents
                .AsNoTracking()
                .AnyAsync(e => e.EventId == eventId);

            if (alreadyExists)
            {
                return false;
            }
        }

        var existing = await _context.ProcessedStripeWebhookEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (existing != null)
        {
            if (existing.ProcessedAt.HasValue)
            {
                return false;
            }

            if (existing.ProcessingStartedAt.HasValue &&
                existing.ProcessingStartedAt.Value > staleProcessingCutoff)
            {
                return false;
            }

            existing.EventType = eventType;
            existing.ProcessingStartedAt = now;
            existing.ErrorMessage = null;
            await _context.SaveChangesAsync();
            return true;
        }

        _context.ProcessedStripeWebhookEvents.Add(new ProcessedStripeWebhookEvent
        {
            EventId = eventId,
            EventType = eventType,
            ReceivedAt = now,
            ProcessingStartedAt = now
        });

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task MarkWebhookEventProcessedAsync(string eventId, string eventType)
    {
        var webhookEvent = await _context.ProcessedStripeWebhookEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (webhookEvent == null)
        {
            return;
        }

        webhookEvent.EventType = eventType;
        webhookEvent.ProcessedAt = DateTime.UtcNow;
        webhookEvent.ErrorMessage = null;
        await _context.SaveChangesAsync();
    }

    private async Task MarkWebhookEventFailedAsync(string eventId, string eventType, Exception ex)
    {
        var webhookEvent = await _context.ProcessedStripeWebhookEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (webhookEvent == null)
        {
            return;
        }

        webhookEvent.EventType = eventType;
        webhookEvent.ProcessingStartedAt = null;
        webhookEvent.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
        await _context.SaveChangesAsync();
    }

    private async Task TryLogAuditEventAsync(
        Guid userId,
        string action,
        string ipAddress,
        string? userAgent,
        bool success,
        string details)
    {
        try
        {
            await _auditLogService.LogEventAsync(userId, action, ipAddress, userAgent, success, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write non-critical audit event {Action} for user {UserId}", action, userId);
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            _logger.LogWarning("PaymentIntent object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        _logger.LogInformation("Payment succeeded: {PaymentIntentId}, Amount: {Amount}, Customer: {Customer}",
            paymentIntent.Id, paymentIntent.Amount, paymentIntent.CustomerId);

        try
        {
            // Find any pending transactions with this external transaction ID
            var existingTransaction = await _context.SubscriptionTransactions
                .FirstOrDefaultAsync(t => t.ExternalTransactionId == paymentIntent.Id);

            if (existingTransaction != null)
            {
                // Update existing transaction to completed
                existingTransaction.Status = TransactionStatus.Completed;
                existingTransaction.ProcessedAt = DateTime.UtcNow;
                existingTransaction.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated transaction {TransactionId} to Completed for payment intent {PaymentIntentId}",
                    existingTransaction.Id, paymentIntent.Id);
            }
            else
            {
                // If no existing transaction, this might be a one-time payment outside subscription
                // Try to find user by Stripe customer ID
                if (!string.IsNullOrEmpty(paymentIntent.CustomerId))
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.ExternalCustomerId == paymentIntent.CustomerId);

                    if (user != null)
                    {
                        await TryLogAuditEventAsync(
                            user.Id,
                            "PAYMENT_SUCCEEDED",
                            "Webhook",
                            null,
                            true,
                            $"Payment intent succeeded: ${paymentIntent.Amount / 100m:F2}");
                    }
                }
            }

            _logger.LogInformation("Successfully processed payment_intent.succeeded for {PaymentIntentId}", paymentIntent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment_intent.succeeded for {PaymentIntentId}", paymentIntent.Id);
            throw;
        }
    }

    private async Task HandlePaymentIntentFailedAsync(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            _logger.LogWarning("PaymentIntent object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        var failureReason = paymentIntent.LastPaymentError?.Message ?? "Unknown";
        _logger.LogWarning("Payment failed: {PaymentIntentId}, Reason: {FailureMessage}, Customer: {Customer}",
            paymentIntent.Id, failureReason, paymentIntent.CustomerId);

        try
        {
            // Find any pending transactions with this external transaction ID
            var existingTransaction = await _context.SubscriptionTransactions
                .Include(t => t.Subscription)
                .FirstOrDefaultAsync(t => t.ExternalTransactionId == paymentIntent.Id);

            if (existingTransaction != null)
            {
                // Update transaction to failed
                existingTransaction.Status = TransactionStatus.Failed;
                existingTransaction.FailedAt = DateTime.UtcNow;
                existingTransaction.FailureReason = failureReason;
                existingTransaction.RetryCount++;
                existingTransaction.NextRetryAt = DateTime.UtcNow.AddDays(1); // Retry in 1 day

                // Update subscription status to PastDue if this was a subscription payment
                if (existingTransaction.Subscription != null)
                {
                    existingTransaction.Subscription.Status = SubscriptionStatus.PastDue;
                    existingTransaction.Subscription.RetryCount++;
                    existingTransaction.Subscription.NextRetryAt = DateTime.UtcNow.AddDays(1);
                    existingTransaction.Subscription.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated transaction {TransactionId} to Failed for payment intent {PaymentIntentId}",
                    existingTransaction.Id, paymentIntent.Id);

                // Log audit event
                await TryLogAuditEventAsync(
                    existingTransaction.UserId,
                    "PAYMENT_FAILED",
                    "Webhook",
                    null,
                    false,
                    $"Payment failed: {failureReason}");
            }
            else if (!string.IsNullOrEmpty(paymentIntent.CustomerId))
            {
                // Find user by Stripe customer ID and log audit event
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ExternalCustomerId == paymentIntent.CustomerId);

                if (user != null)
                {
                    // Check if user has an active subscription and mark it as past due
                    var subscription = await _context.UserSubscriptions
                        .Where(s => s.UserId == user.Id &&
                                   (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
                        .FirstOrDefaultAsync();

                    if (subscription != null)
                    {
                        subscription.Status = SubscriptionStatus.PastDue;
                        subscription.RetryCount++;
                        subscription.NextRetryAt = DateTime.UtcNow.AddDays(1);
                        subscription.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    await TryLogAuditEventAsync(
                        user.Id,
                        "PAYMENT_FAILED",
                        "Webhook",
                        null,
                        false,
                        $"Payment intent failed: {failureReason}");
                }
            }

            _logger.LogInformation("Successfully processed payment_intent.payment_failed for {PaymentIntentId}", paymentIntent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment_intent.payment_failed for {PaymentIntentId}", paymentIntent.Id);
            throw;
        }
    }

    private async Task HandleSubscriptionEventAsync(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null)
        {
            _logger.LogWarning("Subscription object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        _logger.LogInformation("Subscription event: {EventType}, Subscription: {SubscriptionId}, Status: {Status}",
            stripeEvent.Type, stripeSubscription.Id, stripeSubscription.Status);

        try
        {
            // Find our subscription by Stripe subscription ID
            var subscription = await _context.UserSubscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == stripeSubscription.Id);

            if (subscription == null)
            {
                _logger.LogWarning("No local subscription found for Stripe subscription {StripeSubId}", stripeSubscription.Id);
                return;
            }

            // Map Stripe status to our status
            var previousStatus = subscription.Status;
            var stripeStatus = ExtractStringFromRawObject(stripeEvent, "status") ?? stripeSubscription.Status;
            subscription.Status = MapStripeStatusToLocal(stripeStatus);
            subscription.UpdatedAt = DateTime.UtcNow;

            // Handle specific event types
            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                    // Subscription already created via checkout - just log
                    await TryLogAuditEventAsync(
                        subscription.UserId,
                        "STRIPE_SUBSCRIPTION_CREATED",
                        "Webhook",
                        null,
                        true,
                        $"Stripe subscription created: {stripeSubscription.Id}");
                    break;

                case "customer.subscription.updated":
                    // Handle status changes
                    if (stripeSubscription.CancelAtPeriodEnd)
                    {
                        subscription.AutoRenew = false;
                        // Get billing period end from Stripe subscription items
                        var periodEnd = GetSubscriptionPeriodEnd(stripeSubscription);
                        subscription.EndDate = periodEnd;
                    }

                    // Update billing dates from Stripe subscription items
                    subscription.NextBillingDate = GetSubscriptionPeriodEnd(stripeSubscription);

                    await TryLogAuditEventAsync(
                        subscription.UserId,
                        "SUBSCRIPTION_UPDATED_VIA_STRIPE",
                        "Webhook",
                        null,
                        true,
                        $"Status changed from {previousStatus} to {subscription.Status}");
                    break;

                case "customer.subscription.deleted":
                    subscription.Status = SubscriptionStatus.Cancelled;
                    subscription.CancelledAt = DateTime.UtcNow;
                    subscription.EndDate = DateTime.UtcNow;

                    await TryLogAuditEventAsync(
                        subscription.UserId,
                        "SUBSCRIPTION_CANCELLED_VIA_STRIPE",
                        "Webhook",
                        null,
                        true,
                        $"Subscription deleted in Stripe");
                    break;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully processed subscription event {EventType} for {SubscriptionId}",
                stripeEvent.Type, stripeSubscription.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription event {EventType} for {SubscriptionId}",
                stripeEvent.Type, stripeSubscription.Id);
            throw;
        }
    }

    /// <summary>
    /// Maps Stripe subscription status to our internal SubscriptionStatus enum
    /// </summary>
    private static SubscriptionStatus MapStripeStatusToLocal(string stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trial,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            "unpaid" => SubscriptionStatus.Suspended,
            "incomplete" => SubscriptionStatus.PastDue,
            "incomplete_expired" => SubscriptionStatus.Expired,
            _ => SubscriptionStatus.Suspended
        };
    }

    /// <summary>
    /// Gets the current period end from a Stripe subscription
    /// In Stripe.NET v49+, this is accessed via Items.Data[0].CurrentPeriodEnd or similar
    /// </summary>
    private static DateTime? GetSubscriptionPeriodEnd(Stripe.Subscription stripeSubscription)
    {
        // Try to get from subscription items first
        if (stripeSubscription.Items?.Data != null && stripeSubscription.Items.Data.Count > 0)
        {
            var firstItem = stripeSubscription.Items.Data[0];
            // In v49+, the period end is at the item level
            return firstItem.CurrentPeriodEnd;
        }

        // Fallback: Calculate from start + interval
        return DateTime.UtcNow.AddMonths(1);
    }

    /// <summary>
    /// Extracts subscription ID from a Stripe event's raw JSON data
    /// Some events have subscription as a string reference rather than an expanded object
    /// </summary>
    private static string? ExtractSubscriptionIdFromRawJson(Event stripeEvent)
    {
        try
        {
            var rawObject = stripeEvent.Data.RawObject;
            if (rawObject.HasValue)
            {
                System.Text.Json.JsonElement subProp;
                if (rawObject.Value.TryGetProperty("subscription", out subProp))
                {
                    if (subProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return subProp.GetString();
                    }
                }
            }
        }
        catch
        {
            // Silently fail if we can't extract the subscription ID
        }
        return null;
    }

    private static string? ExtractStringFromRawObject(Event stripeEvent, string propertyName)
    {
        try
        {
            var rawObject = stripeEvent.Data.RawObject;
            if (!rawObject.HasValue)
            {
                return null;
            }

            if (rawObject.Value.TryGetProperty(propertyName, out System.Text.Json.JsonElement property) &&
                property.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return property.GetString();
            }
        }
        catch
        {
            // Ignore malformed raw payload fragments and fall back to Stripe SDK properties.
        }

        return null;
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Invoice object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        _logger.LogInformation("Invoice payment succeeded: {InvoiceId}, Amount: {Amount}, Customer: {Customer}",
            invoice.Id, invoice.AmountPaid, invoice.CustomerId);

        try
        {
            // Extract subscription ID from raw JSON since it's a string reference
            string? subscriptionId = ExtractSubscriptionIdFromRawJson(stripeEvent);

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                // Use the subscription service's RecordPaymentAsync
                await _subscriptionService.RecordPaymentAsync(subscriptionId, invoice.AmountPaid);
            }
            else if (!string.IsNullOrEmpty(invoice.CustomerId))
            {
                // One-time invoice payment (not subscription)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ExternalCustomerId == invoice.CustomerId);

                if (user != null)
                {
                    await TryLogAuditEventAsync(
                        user.Id,
                        "INVOICE_PAYMENT_SUCCEEDED",
                        "Webhook",
                        null,
                        true,
                        $"Invoice payment succeeded: ${invoice.AmountPaid / 100m:F2}");
                }
            }

            _logger.LogInformation("Successfully processed invoice.payment_succeeded for {InvoiceId}", invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice.payment_succeeded for {InvoiceId}", invoice.Id);
            throw;
        }
    }

    private async Task HandleInvoicePaymentFailedAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Invoice object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        _logger.LogWarning("Invoice payment failed: {InvoiceId}, Customer: {Customer}, Attempt: {AttemptCount}",
            invoice.Id, invoice.CustomerId, invoice.AttemptCount);

        try
        {
            // Extract subscription ID from raw JSON
            string? subscriptionId = ExtractSubscriptionIdFromRawJson(stripeEvent);

            UserSubscription? subscription = null;
            Guid? userId = null;

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                // Find subscription by Stripe ID
                subscription = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

                if (subscription != null)
                {
                    userId = subscription.UserId;
                }
            }
            else if (!string.IsNullOrEmpty(invoice.CustomerId))
            {
                // Find user by Stripe customer ID
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ExternalCustomerId == invoice.CustomerId);

                if (user != null)
                {
                    userId = user.Id;
                    // Find their active subscription
                    subscription = await _context.UserSubscriptions
                        .Where(s => s.UserId == user.Id &&
                                   (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
                        .FirstOrDefaultAsync();
                }
            }

            if (subscription != null)
            {
                // Update subscription to past due and set up retry
                subscription.Status = SubscriptionStatus.PastDue;
                // AttemptCount is a long, cast to int for our retry count
                subscription.RetryCount = invoice.AttemptCount > 0 ? (int)invoice.AttemptCount : subscription.RetryCount + 1;
                subscription.UpdatedAt = DateTime.UtcNow;

                // Calculate next retry date based on dunning strategy
                // Default: 3 days after first failure, then 5 days, then 7 days
                var daysUntilRetry = subscription.RetryCount switch
                {
                    1 => 3,
                    2 => 5,
                    3 => 7,
                    _ => 7
                };
                subscription.NextRetryAt = DateTime.UtcNow.AddDays(daysUntilRetry);

                // If too many failed attempts, suspend the subscription
                if (subscription.RetryCount >= 4)
                {
                    subscription.Status = SubscriptionStatus.Suspended;
                    _logger.LogWarning("Subscription {SubscriptionId} suspended after {RetryCount} failed payment attempts",
                        subscription.Id, subscription.RetryCount);
                }

                await _context.SaveChangesAsync();
            }

            if (userId.HasValue)
            {
                await TryLogAuditEventAsync(
                    userId.Value,
                    "INVOICE_PAYMENT_FAILED",
                    "Webhook",
                    null,
                    false,
                    $"Invoice payment failed. Attempt #{invoice.AttemptCount}. Next retry scheduled.");
            }

            _logger.LogInformation("Successfully processed invoice.payment_failed for {InvoiceId}", invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice.payment_failed for {InvoiceId}", invoice.Id);
            throw;
        }
    }

    private async Task HandleChargeRefundedAsync(Event stripeEvent)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null)
        {
            _logger.LogWarning("Charge object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        _logger.LogInformation("Charge refunded: {ChargeId}, Amount: {AmountRefunded}, Customer: {Customer}",
            charge.Id, charge.AmountRefunded, charge.CustomerId);

        try
        {
            var cumulativeRefundAmount = Math.Abs(charge.AmountRefunded / 100m);
            if (cumulativeRefundAmount <= 0)
            {
                _logger.LogWarning("Charge refund event {EventId} for charge {ChargeId} has no refunded amount",
                    stripeEvent.Id, charge.Id);
                return;
            }

            Guid? userId;
            decimal refundAmount;
            var chargeLock = ChargeRefundLocks.GetOrAdd(charge.Id, _ => new SemaphoreSlim(1, 1));
            await chargeLock.WaitAsync();

            try
            {
                if (_context.Database.IsRelational())
                {
                    var strategy = _context.Database.CreateExecutionStrategy();
                    (userId, refundAmount) = await strategy.ExecuteAsync(async () =>
                    {
                        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                        await AcquireChargeRefundDatabaseLockAsync(charge.Id);
                        var result = await ProcessChargeRefundAccountingAsync(stripeEvent, charge, cumulativeRefundAmount);
                        await transaction.CommitAsync();
                        return result;
                    });
                }
                else
                {
                    (userId, refundAmount) = await ProcessChargeRefundAccountingAsync(stripeEvent, charge, cumulativeRefundAmount);
                }
            }
            finally
            {
                chargeLock.Release();
                if (chargeLock.CurrentCount == 1)
                    ChargeRefundLocks.TryRemove(charge.Id, out _);
            }

            if (userId.HasValue)
            {
                await TryLogAuditEventAsync(
                    userId.Value,
                    "CHARGE_REFUNDED",
                    "Webhook",
                    null,
                    true,
                    $"Refund processed: ${refundAmount:F2}");
            }

            _logger.LogInformation("Successfully processed charge.refunded for {ChargeId}", charge.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing charge.refunded for {ChargeId}", charge.Id);
            throw;
        }
    }

    private async Task AcquireChargeRefundDatabaseLockAsync(string chargeId)
    {
        var providerName = _context.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", chargeId);
        }
    }

    private async Task<(Guid? UserId, decimal RefundAmount)> ProcessChargeRefundAccountingAsync(
        Event stripeEvent,
        Charge charge,
        decimal cumulativeRefundAmount)
    {
        var purchaseRefundAmount = await _context.SubscriptionTransactions
            .Where(t => t.Type != SubscriptionTransactionType.Refund && t.ExternalChargeId == charge.Id)
            .MaxAsync(t => (decimal?)t.RefundAmount) ?? 0m;
        var recordedRefundAmount = await _context.SubscriptionTransactions
            .Where(t => t.Type == SubscriptionTransactionType.Refund && t.ExternalChargeId == charge.Id)
            .SumAsync(t => (decimal?)t.RefundAmount ?? t.Amount);
        recordedRefundAmount = Math.Max(recordedRefundAmount, purchaseRefundAmount);
        var refundAmount = cumulativeRefundAmount - recordedRefundAmount;

        if (refundAmount <= 0)
        {
            _logger.LogInformation(
                "Skipping refund event {EventId} for charge {ChargeId}; cumulative amount {CumulativeRefundAmount} is already recorded",
                stripeEvent.Id, charge.Id, cumulativeRefundAmount);
            return (null, 0m);
        }

        var refundExternalId = !string.IsNullOrWhiteSpace(stripeEvent.Id)
            ? stripeEvent.Id
            : $"{charge.Id}:{cumulativeRefundAmount:F2}";

        var existingTransaction = await _context.SubscriptionTransactions
            .Include(t => t.Subscription)
            .FirstOrDefaultAsync(t => t.ExternalChargeId == charge.Id || t.ExternalTransactionId == charge.PaymentIntentId);

        if (existingTransaction != null)
        {
            var refundTransaction = CreateRefundTransaction(
                existingTransaction.SubscriptionId,
                existingTransaction.UserId,
                charge,
                refundExternalId,
                refundAmount);

            _context.SubscriptionTransactions.Add(refundTransaction);

            existingTransaction.RefundedAt = DateTime.UtcNow;
            existingTransaction.RefundAmount = cumulativeRefundAmount;
            existingTransaction.Status = charge.Refunded ? TransactionStatus.Reversed : existingTransaction.Status;

            if (charge.Refunded && existingTransaction.Subscription != null)
            {
                var daysSincePayment = (DateTime.UtcNow - existingTransaction.CreatedAt).TotalDays;
                if (daysSincePayment <= 30)
                {
                    _logger.LogInformation("Full refund issued within 30 days. Subscription may need attention: {SubscriptionId}",
                        existingTransaction.SubscriptionId);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created refund transaction {TransactionId} for charge {ChargeId}",
                refundTransaction.Id, charge.Id);
            return (existingTransaction.UserId, refundAmount);
        }

        if (!string.IsNullOrEmpty(charge.CustomerId))
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ExternalCustomerId == charge.CustomerId);

            if (user != null)
            {
                var subscription = await _context.UserSubscriptions
                    .Where(s => s.UserId == user.Id)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    var refundTransaction = CreateRefundTransaction(
                        subscription.Id,
                        user.Id,
                        charge,
                        refundExternalId,
                        refundAmount);

                    _context.SubscriptionTransactions.Add(refundTransaction);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created refund transaction {TransactionId} for charge {ChargeId}",
                        refundTransaction.Id, charge.Id);
                    return (user.Id, refundAmount);
                }
            }
        }

        throw new InvalidOperationException(
            $"Refund for charge {charge.Id} could not be matched to a local subscription transaction or customer subscription.");
    }

    private static SubscriptionTransaction CreateRefundTransaction(
        Guid subscriptionId,
        Guid userId,
        Charge charge,
        string refundExternalId,
        decimal refundAmount)
    {
        return new SubscriptionTransaction
        {
            SubscriptionId = subscriptionId,
            UserId = userId,
            Type = SubscriptionTransactionType.Refund,
            Amount = refundAmount,
            Currency = charge.Currency?.ToUpper() ?? "USD",
            ExternalTransactionId = refundExternalId,
            ExternalChargeId = charge.Id,
            Status = TransactionStatus.Completed,
            Description = $"Refund for charge {charge.Id}",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            RefundedAt = DateTime.UtcNow,
            RefundAmount = refundAmount,
            CreatedFromIP = "Webhook"
        };
    }

    /// <summary>
    /// CRITICAL: Handles checkout.session.completed event
    /// This is fired when a customer successfully completes checkout with payment
    /// This handles both subscription creation and payment method setup
    /// </summary>
    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogError("Checkout session object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        _logger.LogInformation("Checkout session completed: {SessionId}, Mode: {Mode}, Status: {Status}",
            session.Id, session.Mode, session.PaymentStatus);

        // Handle setup mode (payment method setup)
        if (session.Mode == "setup")
        {
            await HandlePaymentMethodSetupCompletedAsync(session);
            return;
        }

        // Only process completed payments for subscription mode
        if (session.PaymentStatus != "paid")
        {
            _logger.LogWarning("Checkout session {SessionId} payment status is {Status}, not 'paid'. Skipping.",
                session.Id, session.PaymentStatus);
            return;
        }

        // Extract metadata from the session
        if (!session.Metadata.TryGetValue("user_id", out var userIdString) ||
            !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogError("User ID not found or invalid in checkout session metadata. SessionId: {SessionId}",
                session.Id);
            return;
        }

        if (!session.Metadata.TryGetValue("tier_id", out var tierIdString) ||
            !Guid.TryParse(tierIdString, out var tierId))
        {
            _logger.LogError("Tier ID not found or invalid in checkout session metadata. SessionId: {SessionId}",
                session.Id);
            return;
        }

        // Get billing cycle from metadata
        var billingCycle = BillingCycle.Monthly;
        if (session.Metadata.TryGetValue("billing_cycle", out var billingCycleString))
        {
            Enum.TryParse<BillingCycle>(billingCycleString, out billingCycle);
        }

        try
        {
            // Extract promotion/discount information if present
            var promotionInfo = await ExtractPromotionInfoAsync(session);

            // Create subscription in our database with promotion info
            var subscription = await _subscriptionService.CreateSubscriptionAsync(
                userId,
                tierId,
                session.SubscriptionId, // Stripe subscription ID
                session.CustomerId,     // Stripe customer ID
                promotionInfo);         // Promotion info (if any)

            _logger.LogInformation(
                "Successfully created subscription {SubscriptionId} for user {UserId} with tier {TierId}. Stripe subscription: {StripeSubscriptionId}. Promo: {PromoCode}",
                subscription.Id, userId, tierId, session.SubscriptionId, promotionInfo?.PromoCode ?? "none");

            // Also sync the payment method from the subscription
            await SyncPaymentMethodFromSubscriptionAsync(userId, session.SubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create subscription for user {UserId} with tier {TierId} from checkout session {SessionId}",
                userId, tierId, session.Id);
            throw;
        }
    }

    /// <summary>
    /// Syncs the payment method from a Stripe subscription to our database
    /// </summary>
    private async Task SyncPaymentMethodFromSubscriptionAsync(Guid userId, string? stripeSubscriptionId)
    {
        if (string.IsNullOrEmpty(stripeSubscriptionId))
        {
            _logger.LogWarning("Cannot sync payment method: Stripe subscription ID is null");
            return;
        }

        // Skip Stripe API calls in test mode
        if (!_stripeSettings.IsEnabled)
        {
            _logger.LogInformation("Stripe is disabled - skipping payment method sync in test mode for subscription {SubscriptionId}", stripeSubscriptionId);
            return;
        }

        try
        {
            // Get the subscription from Stripe to find the default payment method
            var subscriptionService = new Stripe.SubscriptionService();
            var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);

            var paymentMethodId = stripeSubscription.DefaultPaymentMethodId;
            if (string.IsNullOrEmpty(paymentMethodId))
            {
                _logger.LogInformation("No default payment method on subscription {SubscriptionId}", stripeSubscriptionId);
                return;
            }

            // Get payment method details from Stripe
            var paymentMethodService = new Stripe.PaymentMethodService();
            var stripePaymentMethod = await paymentMethodService.GetAsync(paymentMethodId);

            // Create payment method in our database
            var paymentMethod = new Core.Entities.PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "stripe",
                Type = stripePaymentMethod.Type ?? "card",
                Token = stripePaymentMethod.Id,
                Last4Digits = stripePaymentMethod.Card?.Last4 ?? "****",
                Brand = stripePaymentMethod.Card?.Brand ?? "unknown",
                ExpiryDate = stripePaymentMethod.Card != null
                    ? $"{stripePaymentMethod.Card.ExpMonth:D2}/{stripePaymentMethod.Card.ExpYear}"
                    : null,
                CardholderName = stripePaymentMethod.BillingDetails?.Name,
                BillingCountry = stripePaymentMethod.BillingDetails?.Address?.Country,
                BillingPostalCode = stripePaymentMethod.BillingDetails?.Address?.PostalCode,
                IsDefault = true, // Payment method from subscription is the default
                IsValid = true,
                ExpiresAt = stripePaymentMethod.Card != null
                    ? new DateTime(
                        (int)stripePaymentMethod.Card.ExpYear,
                        (int)stripePaymentMethod.Card.ExpMonth,
                        DateTime.DaysInMonth((int)stripePaymentMethod.Card.ExpYear, (int)stripePaymentMethod.Card.ExpMonth))
                    : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            await _paymentService.SavePaymentMethodFromWebhookAsync(paymentMethod);

            _logger.LogInformation(
                "Successfully synced payment method {PaymentMethodId} for user {UserId} from subscription {SubscriptionId}. Card: ****{Last4}",
                paymentMethod.Id, userId, stripeSubscriptionId, paymentMethod.Last4Digits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to sync payment method for user {UserId} from subscription {SubscriptionId}",
                userId, stripeSubscriptionId);
            // Don't throw - we don't want to fail the subscription creation for this
        }
    }

    /// <summary>
    /// Handles payment method setup completion from checkout.session.completed with mode='setup'
    /// Syncs the payment method from Stripe to our database
    /// </summary>
    private async Task HandlePaymentMethodSetupCompletedAsync(Session session)
    {
        // Extract user ID from metadata
        if (!session.Metadata.TryGetValue("user_id", out var userIdString) ||
            !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogError("User ID not found in payment method setup session. SessionId: {SessionId}", session.Id);
            return;
        }

        try
        {
            // Get the SetupIntent from the session
            var setupIntentId = session.SetupIntentId;
            if (string.IsNullOrEmpty(setupIntentId))
            {
                _logger.LogError("SetupIntent ID not found in session {SessionId}", session.Id);
                return;
            }

            // Skip Stripe API calls in test mode
            if (!_stripeSettings.IsEnabled)
            {
                _logger.LogInformation("Stripe is disabled - skipping payment method setup in test mode for session {SessionId}", session.Id);
                return;
            }

            // Retrieve the SetupIntent to get the payment method
            var setupIntentService = new Stripe.SetupIntentService();
            var setupIntent = await setupIntentService.GetAsync(setupIntentId);

            if (string.IsNullOrEmpty(setupIntent.PaymentMethodId))
            {
                _logger.LogError("Payment method ID not found in SetupIntent {SetupIntentId}", setupIntentId);
                return;
            }

            // Get payment method details from Stripe
            var paymentMethodService = new Stripe.PaymentMethodService();
            var stripePaymentMethod = await paymentMethodService.GetAsync(setupIntent.PaymentMethodId);

            // Create payment method in our database
            var paymentMethod = new Core.Entities.PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "stripe",
                Type = stripePaymentMethod.Type ?? "card",
                Token = stripePaymentMethod.Id, // Stripe payment method ID
                Last4Digits = stripePaymentMethod.Card?.Last4 ?? "****",
                Brand = stripePaymentMethod.Card?.Brand ?? "unknown",
                ExpiryDate = stripePaymentMethod.Card != null
                    ? $"{stripePaymentMethod.Card.ExpMonth:D2}/{stripePaymentMethod.Card.ExpYear}"
                    : null,
                CardholderName = stripePaymentMethod.BillingDetails?.Name,
                BillingCountry = stripePaymentMethod.BillingDetails?.Address?.Country,
                BillingPostalCode = stripePaymentMethod.BillingDetails?.Address?.PostalCode,
                IsDefault = false,  // Will be set as default if it's the first one
                IsValid = true,
                ExpiresAt = stripePaymentMethod.Card != null
                    ? new DateTime(
                        (int)stripePaymentMethod.Card.ExpYear,
                        (int)stripePaymentMethod.Card.ExpMonth,
                        DateTime.DaysInMonth((int)stripePaymentMethod.Card.ExpYear, (int)stripePaymentMethod.Card.ExpMonth))
                    : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Check if this is the first payment method for the user
            var existingPaymentMethods = await _paymentService.GetUserPaymentMethodsAsync(userId);
            if (!existingPaymentMethods.Any())
            {
                paymentMethod.IsDefault = true;
            }

            // Save to database
            await _paymentService.SavePaymentMethodFromWebhookAsync(paymentMethod);

            _logger.LogInformation(
                "Successfully synced payment method {PaymentMethodId} for user {UserId} from Stripe. Card: ****{Last4}",
                paymentMethod.Id, userId, paymentMethod.Last4Digits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to sync payment method for user {UserId} from session {SessionId}",
                userId, session.Id);
            throw;
        }
    }

    /// <summary>
    /// Extracts promotion/discount information from a Stripe checkout session
    /// </summary>
    private async Task<SubscriptionPromotionInfo?> ExtractPromotionInfoAsync(Session session)
    {
        try
        {
            // Check if there's a subscription to get discount info from
            if (string.IsNullOrEmpty(session.SubscriptionId))
            {
                return null;
            }

            // Skip Stripe API calls in test mode
            if (!_stripeSettings.IsEnabled)
            {
                _logger.LogInformation("Stripe is disabled - skipping promotion info extraction in test mode for subscription {SubscriptionId}", session.SubscriptionId);
                return null;
            }

            // Get the subscription with discounts expanded
            // In Stripe.NET v49+, coupon is in Source.Coupon
            var subscriptionService = new Stripe.SubscriptionService();
            var stripeSubscription = await subscriptionService.GetAsync(session.SubscriptionId, new SubscriptionGetOptions
            {
                Expand = new List<string> { "discounts", "discounts.source.coupon", "discounts.promotion_code" }
            });

            // Stripe.NET v49+ uses Discounts (plural) instead of Discount
            var discounts = stripeSubscription.Discounts;
            if (discounts == null || !discounts.Any())
            {
                _logger.LogInformation("No discount applied to subscription {SubscriptionId}", session.SubscriptionId);
                return null;
            }

            var discount = discounts.First();
            // In Stripe.NET v49+, coupon is accessed via Source.Coupon
            var coupon = discount.Source?.Coupon;

            if (coupon == null)
            {
                _logger.LogWarning("Discount exists but coupon is null for subscription {SubscriptionId}", session.SubscriptionId);
                return null;
            }

            // Calculate discount end date based on coupon duration
            DateTime? discountEndsAt = null;
            if (coupon.Duration == "repeating" && coupon.DurationInMonths.HasValue)
            {
                discountEndsAt = DateTime.UtcNow.AddMonths((int)coupon.DurationInMonths.Value);
            }
            else if (coupon.Duration == "once")
            {
                // One-time discount - ends after first billing
                discountEndsAt = DateTime.UtcNow.AddMonths(1);
            }
            // "forever" has no end date

            var promotionInfo = new SubscriptionPromotionInfo
            {
                CouponId = coupon.Id,
                // In Stripe.NET v49+, PromotionCode is still directly on discount
                PromoCode = discount.PromotionCode?.Code,
                PercentOff = coupon.PercentOff,
                AmountOff = coupon.AmountOff,
                Duration = coupon.Duration,
                DurationInMonths = (int?)coupon.DurationInMonths,
                DiscountEndsAt = discountEndsAt
            };

            _logger.LogInformation(
                "Extracted promotion info for subscription {SubscriptionId}: Coupon={CouponId}, PromoCode={PromoCode}, Discount={Discount}, EndsAt={EndsAt}",
                session.SubscriptionId,
                promotionInfo.CouponId,
                promotionInfo.PromoCode ?? "N/A",
                promotionInfo.PercentOff.HasValue ? $"{promotionInfo.PercentOff}% off" : $"${promotionInfo.AmountOff / 100m:F2} off",
                promotionInfo.DiscountEndsAt?.ToString("yyyy-MM-dd") ?? "never");

            return promotionInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract promotion info from session {SessionId}", session.Id);
            // Don't fail the subscription creation if we can't extract promo info
            return null;
        }
    }

    /// <summary>
    /// Handles invoice.paid event - confirms payment was collected
    /// </summary>
    private async Task HandleInvoicePaidAsync(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            _logger.LogWarning("Invoice object is null in event {EventId}", stripeEvent.Id);
            return;
        }

        // Access subscription ID - in Stripe.net the raw data contains subscription as string
        // We need to access it from raw JSON data
        string? subscriptionId = null;
        try
        {
            // Try to get subscription ID from the raw JSON data
            var rawObject = stripeEvent.Data.RawObject;
            if (rawObject.HasValue)
            {
                System.Text.Json.JsonElement subProp;
                if (rawObject.Value.TryGetProperty("subscription", out subProp) && subProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    subscriptionId = subProp.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract subscription ID from invoice");
        }

        _logger.LogInformation("Invoice paid: {InvoiceId}, Amount: {AmountPaid}, Subscription: {SubscriptionId}",
            invoice.Id, invoice.AmountPaid, subscriptionId);

        // For recurring payments, update the subscription's last payment date
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            try
            {
                await _subscriptionService.RecordPaymentAsync(subscriptionId, invoice.AmountPaid);
                _logger.LogInformation("Recorded payment for subscription {SubscriptionId}", subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record payment for subscription {SubscriptionId}", subscriptionId);
                // Don't throw - we don't want to fail the webhook for this
            }
        }
    }
}
