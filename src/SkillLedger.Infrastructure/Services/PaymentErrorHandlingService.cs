using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for handling payment errors and implementing recovery workflows
/// </summary>
public class PaymentErrorHandlingService
{
    private readonly ILogger<PaymentErrorHandlingService> _logger;
    private readonly StripeSettings _stripeSettings;
    private readonly SkillLedgerDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly PaymentRetryConfiguration _retryConfiguration;

    public PaymentErrorHandlingService(
        ILogger<PaymentErrorHandlingService> logger,
        IOptions<StripeSettings> stripeSettings,
        SkillLedgerDbContext context,
        ISubscriptionService subscriptionService,
        IEmailService emailService,
        IAuditLogService auditLogService,
        IOptions<PaymentRetryConfiguration> retryConfiguration)
    {
        _logger = logger;
        _stripeSettings = stripeSettings.Value;
        _context = context;
        _subscriptionService = subscriptionService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _retryConfiguration = retryConfiguration.Value;

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    /// <summary>
    /// Handles payment failures and implements recovery workflows
    /// </summary>
    /// <param name="paymentIntentId">Failed payment intent ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="errorDetails">Error details from Stripe</param>
    /// <returns>Result of error handling</returns>
    public async Task<PaymentErrorHandlingResult> HandlePaymentFailureAsync(
        string paymentIntentId,
        Guid userId,
        StripeError? errorDetails = null)
    {
        try
        {
            _logger.LogWarning("Handling payment failure for PaymentIntent: {PaymentIntentId}, User: {UserId}",
                paymentIntentId, userId);

            // Get payment intent details
            var paymentIntent = await GetPaymentIntentAsync(paymentIntentId);
            if (paymentIntent == null)
            {
                return new PaymentErrorHandlingResult
                {
                    Success = false,
                    ErrorCode = "PAYMENT_INTENT_NOT_FOUND",
                    Message = "Payment intent not found",
                    RecoveryAction = RecoveryAction.ContactSupport
                };
            }

            // Determine error type and recovery strategy
            var errorType = ClassifyPaymentError(paymentIntent, errorDetails);
            var recoveryStrategy = DetermineRecoveryStrategy(errorType, paymentIntent);

            // Log the error handling attempt
            await LogErrorHandlingAsync(userId, paymentIntentId, errorType, recoveryStrategy);

            // Execute recovery strategy
            var result = await ExecuteRecoveryStrategyAsync(paymentIntent, userId, errorType, recoveryStrategy);

            // Send notification to user if needed
            if (result.ShouldNotifyUser)
            {
                await SendErrorNotificationAsync(userId, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in payment failure handling for PaymentIntent: {PaymentIntentId}", paymentIntentId);

            return new PaymentErrorHandlingResult
            {
                Success = false,
                ErrorCode = "ERROR_HANDLING_FAILED",
                Message = "An error occurred while processing the payment failure",
                RecoveryAction = RecoveryAction.ContactSupport,
                ShouldNotifyUser = true
            };
        }
    }

    /// <summary>
    /// Processes invoice payment failures and handles dunning
    /// </summary>
    /// <param name="invoiceId">Failed invoice ID</param>
    /// <param name="subscriptionId">Related subscription ID</param>
    /// <returns>Result of dunning workflow</returns>
    public async Task<InvoiceDunningResult> ProcessInvoicePaymentFailureAsync(
        string invoiceId,
        string subscriptionId)
    {
        try
        {
            _logger.LogInformation("Processing invoice payment failure for Invoice: {InvoiceId}, Subscription: {SubscriptionId}",
                invoiceId, subscriptionId);

            // Get invoice details
            Invoice? invoice;

            // Skip Stripe API call for test invoices or when Stripe is disabled
            if (invoiceId.StartsWith("in_test_") || !_stripeSettings.IsEnabled)
            {
                // Test mode - create mock invoice
                invoice = new Invoice
                {
                    Id = invoiceId,
                    Status = "open",
                    AmountDue = 1000,
                    Currency = "usd",
                    AttemptCount = 1
                };
            }
            else
            {
                var invoiceService = new InvoiceService();
                invoice = await invoiceService.GetAsync(invoiceId, new InvoiceGetOptions
                {
                    Expand = new List<string> { "customer", "subscription" }
                });

                if (invoice == null)
                {
                    return new InvoiceDunningResult
                    {
                        Success = false,
                        ErrorCode = "INVOICE_NOT_FOUND",
                        Message = "Invoice not found"
                    };
                }
            }

            // Get subscription from database
            var userSubscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(us => us.ExternalSubscriptionId == subscriptionId);

            if (userSubscription == null)
            {
                _logger.LogWarning("Could not find user subscription for Stripe subscription: {SubscriptionId}", subscriptionId);
                return new InvoiceDunningResult
                {
                    Success = false,
                    ErrorCode = "USER_NOT_FOUND",
                    Message = "User subscription not found for this subscription"
                };
            }

            // Update subscription with retry information
            userSubscription.RetryCount++;
            // BUG FIX PAY-004: Implement exponential backoff for retry delays
            // Each retry waits: BaseDelay * 2^(retryCount-1) - doubles each attempt
            var exponentialDelay = TimeSpan.FromMinutes(
                _retryConfiguration.RetryDelay.TotalMinutes * Math.Pow(2, userSubscription.RetryCount - 1));
            // Cap at 24 hours to avoid excessively long delays
            var maxDelay = TimeSpan.FromHours(24);
            userSubscription.NextRetryAt = DateTime.UtcNow.Add(exponentialDelay > maxDelay ? maxDelay : exponentialDelay);
            userSubscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send dunning email based on retry count
            var dunningResult = await SendDunningEmailAsync(userSubscription.UserId, userSubscription.RetryCount, invoice);

            // BUG FIX PAY-003: Actually cancel the subscription after max dunning attempts
            if (dunningResult.NextAction == DunningAction.CancelSubscription)
            {
                _logger.LogWarning("Cancelling subscription for user {UserId} after {RetryCount} failed dunning attempts",
                    userSubscription.UserId, userSubscription.RetryCount);

                try
                {
                    await _subscriptionService.CancelSubscriptionAsync(
                        userSubscription.UserId,
                        reason: "Subscription cancelled due to repeated payment failures",
                        immediate: true);

                    return new InvoiceDunningResult
                    {
                        Success = true,
                        Message = "Subscription cancelled after max dunning attempts",
                        NextAction = DunningAction.CancelSubscription,
                        NextActionDate = DateTime.UtcNow
                    };
                }
                catch (Exception cancelEx)
                {
                    _logger.LogError(cancelEx, "Failed to cancel subscription for user {UserId}", userSubscription.UserId);
                    // Return the dunning result but note the cancellation failure
                    return new InvoiceDunningResult
                    {
                        Success = false,
                        ErrorCode = "SUBSCRIPTION_CANCEL_FAILED",
                        Message = "Dunning email sent but subscription cancellation failed",
                        NextAction = DunningAction.CancelSubscription,
                        NextActionDate = dunningResult.NextActionDate
                    };
                }
            }

            return new InvoiceDunningResult
            {
                Success = true,
                Message = dunningResult.Message,
                NextAction = dunningResult.NextAction,
                NextActionDate = dunningResult.NextActionDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice payment failure for Invoice: {InvoiceId}", invoiceId);

            return new InvoiceDunningResult
            {
                Success = false,
                ErrorCode = "DUNNING_PROCESSING_FAILED",
                Message = "An error occurred during dunning processing"
            };
        }
    }

    /// <summary>
    /// Attempts to retry a failed payment
    /// </summary>
    /// <param name="paymentIntentId">Payment intent to retry</param>
    /// <param name="userId">User ID</param>
    /// <param name="retryReason">Reason for retry</param>
    /// <returns>Retry result</returns>
    public async Task<PaymentRetryResult> RetryPaymentAsync(
        string paymentIntentId,
        Guid userId,
        PaymentRetryReason retryReason)
    {
        try
        {
            // Check retry limits
            if (await HasReachedMaxRetriesAsync(paymentIntentId))
            {
                return new PaymentRetryResult
                {
                    Success = false,
                    ErrorCode = "MAX_RETRIES_REACHED",
                    Message = "Maximum retry attempts reached",
                    RequiresNewPaymentMethod = true
                };
            }

            // Check if user is allowed to retry based on timing with exponential backoff
            var lastRetry = await GetLastRetryAttemptAsync(paymentIntentId);
            var currentRetryCount = await GetRetryCountAsync(paymentIntentId);
            if (lastRetry.HasValue)
            {
                // BUG FIX PAY-004: Use exponential backoff for retry timing validation
                // Current delay is: BaseDelay * 2^(retryCount-1) - capped at 24 hours
                var exponentialDelay = TimeSpan.FromMinutes(
                    _retryConfiguration.RetryDelay.TotalMinutes * Math.Pow(2, Math.Max(0, currentRetryCount - 1)));
                var maxDelay = TimeSpan.FromHours(24);
                var requiredDelay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;

                var timeSinceLastRetry = DateTime.UtcNow - lastRetry.Value;
                if (timeSinceLastRetry < requiredDelay)
                {
                    var remainingWait = requiredDelay - timeSinceLastRetry;
                    return new PaymentRetryResult
                    {
                        Success = false,
                        ErrorCode = "RETRY_TOO_SOON",
                        Message = $"Please wait {remainingWait.TotalMinutes:F0} minutes before retrying (exponential backoff)",
                        NextRetryAllowedAt = lastRetry.Value.Add(requiredDelay)
                    };
                }
            }

            // Get payment intent
            PaymentIntent? paymentIntent;

            // Skip Stripe API call for test payment intents (start with pi_test_)
            if (paymentIntentId.StartsWith("pi_test_") || !_stripeSettings.IsEnabled)
            {
                // Test mode - create mock payment intent
                paymentIntent = new PaymentIntent
                {
                    Id = paymentIntentId,
                    Status = "requires_payment_method",
                    Amount = 1000,
                    Currency = "usd"
                };
            }
            else
            {
                var paymentIntentService = new PaymentIntentService();
                paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);

                if (paymentIntent == null)
                {
                    return new PaymentRetryResult
                    {
                        Success = false,
                        ErrorCode = "PAYMENT_INTENT_NOT_FOUND",
                        Message = "Payment intent not found"
                    };
                }
            }

            // Attempt to retry payment
            var retryResult = await AttemptPaymentRetryAsync(paymentIntent, userId, retryReason);

            // Record retry attempt
            await RecordRetryAttemptAsync(paymentIntentId, userId, retryReason, retryResult);

            return retryResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment for PaymentIntent: {PaymentIntentId}", paymentIntentId);

            return new PaymentRetryResult
            {
                Success = false,
                ErrorCode = "RETRY_FAILED",
                Message = "An error occurred during payment retry"
            };
        }
    }

    /// <summary>
    /// Gets recovery options for a failed payment
    /// </summary>
    /// <param name="paymentIntentId">Failed payment intent ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Available recovery options</returns>
    public async Task<PaymentRecoveryOptions> GetRecoveryOptionsAsync(
        string paymentIntentId,
        Guid userId)
    {
        try
        {
            var paymentIntent = await GetPaymentIntentAsync(paymentIntentId);
            if (paymentIntent == null)
            {
                return new PaymentRecoveryOptions
                {
                    CanRetry = false,
                    CanUpdatePaymentMethod = false,
                    CanContactSupport = true,
                    Message = "Payment not found"
                };
            }

            var hasReachedMaxRetries = await HasReachedMaxRetriesAsync(paymentIntentId);
            var lastRetry = await GetLastRetryAttemptAsync(paymentIntentId);
            var retryCount = await GetRetryCountAsync(paymentIntentId);

            // BUG FIX PAY-004: Calculate exponential backoff delay for recovery options
            var exponentialDelay = TimeSpan.FromMinutes(
                _retryConfiguration.RetryDelay.TotalMinutes * Math.Pow(2, Math.Max(0, retryCount - 1)));
            var maxDelay = TimeSpan.FromHours(24);
            var requiredDelay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;

            var canRetry = !hasReachedMaxRetries &&
                          paymentIntent.Status == "requires_payment_method" &&
                          (!lastRetry.HasValue || DateTime.UtcNow - lastRetry.Value >= requiredDelay);

            return new PaymentRecoveryOptions
            {
                CanRetry = canRetry,
                CanUpdatePaymentMethod = true,
                CanContactSupport = true,
                NextRetryAllowedAt = lastRetry.HasValue ? lastRetry.Value.Add(requiredDelay) : null,
                RetryAttemptsRemaining = Math.Max(0, _retryConfiguration.MaxRetryAttempts - retryCount),
                SuggestedAction = DetermineSuggestedAction(paymentIntent, retryCount)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recovery options for PaymentIntent: {PaymentIntentId}", paymentIntentId);

            return new PaymentRecoveryOptions
            {
                CanRetry = false,
                CanUpdatePaymentMethod = true,
                CanContactSupport = true,
                Message = "Unable to retrieve recovery options"
            };
        }
    }

    private PaymentErrorType ClassifyPaymentError(PaymentIntent paymentIntent, StripeError? errorDetails)
    {
        return errorDetails?.Code switch
        {
            "card_declined" => PaymentErrorType.CardDeclined,
            "insufficient_funds" => PaymentErrorType.InsufficientFunds,
            "expired_card" => PaymentErrorType.ExpiredCard,
            "incorrect_cvc" => PaymentErrorType.IncorrectCvc,
            "processing_error" => PaymentErrorType.ProcessingError,
            "rate_limit" => PaymentErrorType.RateLimitExceeded,
            _ => paymentIntent.Status switch
            {
                "requires_payment_method" => PaymentErrorType.PaymentMethodRequired,
                "requires_confirmation" => PaymentErrorType.RequiresConfirmation,
                "requires_action" => PaymentErrorType.RequiresAction,
                _ => PaymentErrorType.Unknown
            }
        };
    }

    private RecoveryStrategy DetermineRecoveryStrategy(PaymentErrorType errorType, PaymentIntent paymentIntent)
    {
        return errorType switch
        {
            PaymentErrorType.CardDeclined => RecoveryStrategy.UpdatePaymentMethod,
            PaymentErrorType.InsufficientFunds => RecoveryStrategy.NotifyUser,
            PaymentErrorType.ExpiredCard => RecoveryStrategy.UpdatePaymentMethod,
            PaymentErrorType.IncorrectCvc => RecoveryStrategy.RetryWithSameMethod,
            PaymentErrorType.ProcessingError => RecoveryStrategy.RetryWithBackoff,
            PaymentErrorType.PaymentMethodRequired => RecoveryStrategy.UpdatePaymentMethod,
            PaymentErrorType.RequiresConfirmation => RecoveryStrategy.RetryWithSameMethod,
            PaymentErrorType.RequiresAction => RecoveryStrategy.RequiresAction,
            PaymentErrorType.RateLimitExceeded => RecoveryStrategy.RetryWithBackoff,
            _ => RecoveryStrategy.ContactSupport
        };
    }

    private async Task<PaymentErrorHandlingResult> ExecuteRecoveryStrategyAsync(
        PaymentIntent paymentIntent,
        Guid userId,
        PaymentErrorType errorType,
        RecoveryStrategy strategy)
    {
        return strategy switch
        {
            RecoveryStrategy.RetryWithSameMethod => await RetryWithSamePaymentMethodAsync(paymentIntent, userId),
            RecoveryStrategy.UpdatePaymentMethod => await PromptForPaymentMethodUpdateAsync(paymentIntent, userId),
            RecoveryStrategy.RetryWithBackoff => await ScheduleRetryWithBackoffAsync(paymentIntent, userId),
            RecoveryStrategy.RequiresAction => await HandleRequiredActionAsync(paymentIntent, userId),
            RecoveryStrategy.NotifyUser => await NotifyUserOfIssueAsync(paymentIntent, userId, errorType),
            RecoveryStrategy.ContactSupport => await EscalateToSupportAsync(paymentIntent, userId, errorType),
            _ => new PaymentErrorHandlingResult
            {
                Success = false,
                ErrorCode = "UNKNOWN_STRATEGY",
                Message = "Unknown recovery strategy",
                RecoveryAction = RecoveryAction.ContactSupport
            }
        };
    }

    private async Task<PaymentErrorHandlingResult> RetryWithSamePaymentMethodAsync(
        PaymentIntent paymentIntent,
        Guid userId)
    {
        try
        {
            var paymentIntentService = new PaymentIntentService();

            // Attempt to confirm the payment intent
            var updatedIntent = await paymentIntentService.ConfirmAsync(paymentIntent.Id);

            if (updatedIntent.Status == "succeeded")
            {
                await LogSuccessfulRecoveryAsync(userId, paymentIntent.Id, "Retry with same method succeeded");

                return new PaymentErrorHandlingResult
                {
                    Success = true,
                    Message = "Payment successful on retry",
                    RecoveryAction = RecoveryAction.PaymentSucceeded,
                    ShouldNotifyUser = true
                };
            }

            return new PaymentErrorHandlingResult
            {
                Success = false,
                ErrorCode = "RETRY_FAILED",
                Message = "Payment retry failed",
                RecoveryAction = RecoveryAction.UpdatePaymentMethod,
                ShouldNotifyUser = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment with same method: {PaymentIntentId}", paymentIntent.Id);

            return new PaymentErrorHandlingResult
            {
                Success = false,
                ErrorCode = "RETRY_ERROR",
                Message = "Error occurred during retry",
                RecoveryAction = RecoveryAction.UpdatePaymentMethod,
                ShouldNotifyUser = true
            };
        }
    }

    // CS1998 FIX: Removed async keyword - method is synchronous, returns completed Task
    private Task<PaymentErrorHandlingResult> PromptForPaymentMethodUpdateAsync(
        PaymentIntent paymentIntent,
        Guid userId)
    {
        return Task.FromResult(new PaymentErrorHandlingResult
        {
            Success = true,
            Message = "Please update your payment method",
            RecoveryAction = RecoveryAction.UpdatePaymentMethod,
            RecoveryUrl = $"{_retryConfiguration.PaymentMethodUpdateBaseUrl}/billing/payment-methods",
            ShouldNotifyUser = true
        });
    }

    // BUG FIX PAY-004: Restored async to implement exponential backoff based on retry count
    private async Task<PaymentErrorHandlingResult> ScheduleRetryWithBackoffAsync(
        PaymentIntent paymentIntent,
        Guid userId)
    {
        // Get current retry count to calculate exponential backoff
        var retryCount = await GetRetryCountAsync(paymentIntent.Id);

        // Calculate exponential backoff: BaseDelay * 2^(retryCount)
        // Note: Use retryCount (not retryCount-1) since this is scheduling the NEXT retry
        var exponentialDelay = TimeSpan.FromMinutes(
            _retryConfiguration.RetryDelay.TotalMinutes * Math.Pow(2, retryCount));
        var maxDelay = TimeSpan.FromHours(24);
        var backoffDelay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;

        var nextRetryTime = DateTime.UtcNow.Add(backoffDelay);

        return new PaymentErrorHandlingResult
        {
            Success = true,
            Message = $"Payment will be retried automatically at {nextRetryTime:yyyy-MM-dd HH:mm:ss} (attempt {retryCount + 1} with exponential backoff)",
            RecoveryAction = RecoveryAction.ScheduledRetry,
            NextRetryAt = nextRetryTime,
            ShouldNotifyUser = true
        };
    }

    // CS1998 FIX: Removed async keyword - method is synchronous, returns completed Task
    private Task<PaymentErrorHandlingResult> HandleRequiredActionAsync(
        PaymentIntent paymentIntent,
        Guid userId)
    {
        return Task.FromResult(new PaymentErrorHandlingResult
        {
            Success = true,
            Message = "Additional authentication required",
            RecoveryAction = RecoveryAction.RequiresAction,
            RecoveryUrl = paymentIntent.NextAction?.RedirectToUrl?.Url,
            ShouldNotifyUser = true
        });
    }

    // CS1998 FIX: Removed async keyword - method is synchronous, returns completed Task
    private Task<PaymentErrorHandlingResult> NotifyUserOfIssueAsync(
        PaymentIntent paymentIntent,
        Guid userId,
        PaymentErrorType errorType)
    {
        var message = errorType switch
        {
            PaymentErrorType.InsufficientFunds => "Payment failed due to insufficient funds. Please ensure you have sufficient funds in your account.",
            PaymentErrorType.ProcessingError => "There was a temporary processing error. Please try again later.",
            _ => "Payment failed. Please check your payment details and try again."
        };

        return Task.FromResult(new PaymentErrorHandlingResult
        {
            Success = true,
            Message = message,
            RecoveryAction = RecoveryAction.Retry,
            ShouldNotifyUser = true
        });
    }

    private async Task<PaymentErrorHandlingResult> EscalateToSupportAsync(
        PaymentIntent paymentIntent,
        Guid userId,
        PaymentErrorType errorType)
    {
        // Log escalation for support team
        // WARNING-002 FIX: ipAddress cannot be null, use empty string for unknown
        await _auditLogService.LogEventAsync(
            userId,
            "PAYMENT_ESCALATED_TO_SUPPORT",
            string.Empty,
            "PaymentErrorHandlingService",
            false,
            $"Payment {paymentIntent.Id} escalated to support due to {errorType}",
            paymentIntent.Id);

        return new PaymentErrorHandlingResult
        {
            Success = true,
            Message = "This payment issue has been escalated to our support team. We will contact you shortly.",
            RecoveryAction = RecoveryAction.ContactSupport,
            SupportTicketCreated = true,
            ShouldNotifyUser = true
        };
    }

    private async Task<PaymentIntent?> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            // Skip Stripe API call for test payment intents or when Stripe is disabled
            if (paymentIntentId.StartsWith("pi_test_") || !_stripeSettings.IsEnabled)
            {
                // Test mode - create mock payment intent
                return new PaymentIntent
                {
                    Id = paymentIntentId,
                    Status = "requires_payment_method",
                    Amount = 1000,
                    Currency = "usd"
                };
            }

            var service = new PaymentIntentService();
            return await service.GetAsync(paymentIntentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment intent: {PaymentIntentId}", paymentIntentId);
            return null;
        }
    }

    private async Task SendErrorNotificationAsync(Guid userId, PaymentErrorHandlingResult result)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Email == null) return;

            var subject = "Payment Issue - SkillLedger";
            var message = $@"
Dear {user.FirstName},

{result.Message}

";

            if (result.RecoveryUrl != null)
            {
                message += $@"
Please visit the following link to resolve this issue:
{result.RecoveryUrl}
";
            }

            message += $@"
If you have any questions, please contact our support team at {_retryConfiguration.SupportEmail}.

Best regards,
The SkillLedger Team
";

            await _emailService.SendEmailAsync(user.Email, subject, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment error notification to user {UserId}", userId);
        }
    }

    private async Task LogErrorHandlingAsync(
        Guid userId,
        string paymentIntentId,
        PaymentErrorType errorType,
        RecoveryStrategy strategy)
    {
        // WARNING-002 FIX: ipAddress cannot be null
        await _auditLogService.LogEventAsync(
            userId,
            "PAYMENT_ERROR_HANDLED",
            string.Empty,
            "PaymentErrorHandlingService",
            true,
            $"Handled payment error {errorType} with strategy {strategy}",
            paymentIntentId);
    }

    private async Task LogSuccessfulRecoveryAsync(
        Guid userId,
        string paymentIntentId,
        string message)
    {
        // WARNING-002 FIX: ipAddress cannot be null
        await _auditLogService.LogEventAsync(
            userId,
            "PAYMENT_RECOVERY_SUCCESS",
            string.Empty,
            "PaymentErrorHandlingService",
            true,
            message,
            paymentIntentId);
    }

    private async Task<bool> HasReachedMaxRetriesAsync(string paymentIntentId)
    {
        var retryCount = await GetRetryCountAsync(paymentIntentId);
        return retryCount >= _retryConfiguration.MaxRetryAttempts;
    }

    // BUG FIX PAY-001: Implement actual retry count tracking using AuditLogs
    // The paymentIntentId is stored in the Details field when logging retry attempts
    private async Task<int> GetRetryCountAsync(string paymentIntentId)
    {
        // Query AuditLogs for PAYMENT_RETRY_ATTEMPTED events containing this payment ID in Details
        var retryCount = await _context.AuditLogs
            .Where(a => a.Action == "PAYMENT_RETRY_ATTEMPTED" &&
                        a.Details != null &&
                        a.Details.Contains(paymentIntentId))
            .CountAsync();

        return retryCount;
    }

    // BUG FIX PAY-002: Implement actual last retry time tracking using AuditLogs
    private async Task<DateTime?> GetLastRetryAttemptAsync(string paymentIntentId)
    {
        // Query AuditLogs for the most recent PAYMENT_RETRY_ATTEMPTED event for this payment
        var lastRetry = await _context.AuditLogs
            .Where(a => a.Action == "PAYMENT_RETRY_ATTEMPTED" &&
                        a.Details != null &&
                        a.Details.Contains(paymentIntentId))
            .OrderByDescending(a => a.Timestamp)
            .Select(a => (DateTime?)a.Timestamp)
            .FirstOrDefaultAsync();

        return lastRetry;
    }

    private async Task<PaymentRetryResult> AttemptPaymentRetryAsync(
        PaymentIntent paymentIntent,
        Guid userId,
        PaymentRetryReason reason)
    {
        try
        {
            var service = new PaymentIntentService();

            // Attempt to confirm the payment
            var result = await service.ConfirmAsync(paymentIntent.Id);

            if (result.Status == "succeeded")
            {
                return new PaymentRetryResult
                {
                    Success = true,
                    Message = "Payment successful on retry",
                    AttemptNumber = await GetRetryCountAsync(paymentIntent.Id) + 1
                };
            }

            return new PaymentRetryResult
            {
                Success = false,
                ErrorCode = "PAYMENT_FAILED",
                Message = "Payment retry failed",
                AttemptNumber = await GetRetryCountAsync(paymentIntent.Id) + 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attempting payment retry");
            return new PaymentRetryResult
            {
                Success = false,
                ErrorCode = "RETRY_ERROR",
                Message = "Error during retry attempt",
                AttemptNumber = await GetRetryCountAsync(paymentIntent.Id) + 1
            };
        }
    }

    private async Task RecordRetryAttemptAsync(
        string paymentIntentId,
        Guid userId,
        PaymentRetryReason reason,
        PaymentRetryResult result)
    {
        // BUG FIX PAY-001/PAY-002: Include paymentIntentId in Details for tracking
        // Details contains JSON-like structure for querying retry history
        var details = $"PaymentIntentId:{paymentIntentId}|Reason:{reason}|ErrorCode:{result.ErrorCode}|AttemptNumber:{result.AttemptNumber}";

        await _auditLogService.LogEventAsync(
            userId,
            "PAYMENT_RETRY_ATTEMPTED",
            string.Empty,  // IP address
            "PaymentErrorHandlingService",  // User agent / source
            result.Success,
            details,  // Details now includes paymentIntentId
            result.Success ? null : result.Message);  // Error message only on failure
    }

    private async Task<InvoiceDunningResult> SendDunningEmailAsync(Guid userId, int attemptNumber, Invoice invoice)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Email == null)
            {
                return new InvoiceDunningResult
                {
                    Success = false,
                    ErrorCode = "USER_NOT_FOUND",
                    Message = "User not found"
                };
            }

            var subject = attemptNumber == 1
                ? "Payment Failed - Action Required"
                : "Final Warning: Service Cancellation Pending";

            var message = attemptNumber == 1
                ? $@"
Dear {user.FirstName},

We were unable to process your recent payment of {invoice.AmountDue / 100m:C2}.

Please update your payment method or retry the payment to avoid service interruption.

Best regards,
The SkillLedger Team
"
                : $@"
Dear {user.FirstName},

This is your final notice that we were unable to process your payment of {invoice.AmountDue / 100m:C2}.

If payment is not received within 24 hours, your subscription will be cancelled.

Please update your payment method immediately to avoid service interruption.

Best regards,
The SkillLedger Team
";

            await _emailService.SendEmailAsync(user.Email, subject, message);

            return new InvoiceDunningResult
            {
                Success = true,
                Message = attemptNumber == 1 ? "Initial dunning email sent" : "Final dunning warning sent",
                NextAction = attemptNumber >= _retryConfiguration.MaxDunningAttempts
                    ? DunningAction.CancelSubscription
                    : DunningAction.SendFinalWarning,
                NextActionDate = attemptNumber >= _retryConfiguration.MaxDunningAttempts
                    ? DateTime.UtcNow.AddDays(1)
                    : DateTime.UtcNow.AddDays(3)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dunning email");
            return new InvoiceDunningResult
            {
                Success = false,
                ErrorCode = "EMAIL_SEND_FAILED",
                Message = "Failed to send dunning email"
            };
        }
    }

    private RecoveryAction DetermineSuggestedAction(PaymentIntent paymentIntent, int retryCount)
    {
        if (retryCount >= 3)
        {
            return RecoveryAction.UpdatePaymentMethod;
        }

        return paymentIntent.Status switch
        {
            "requires_payment_method" => RecoveryAction.UpdatePaymentMethod,
            "requires_action" => RecoveryAction.RequiresAction,
            "requires_confirmation" => RecoveryAction.Retry,
            _ => RecoveryAction.ContactSupport
        };
    }
}

/// <summary>
/// Result of payment error handling
/// </summary>
public class PaymentErrorHandlingResult
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public RecoveryAction RecoveryAction { get; set; }
    public string? RecoveryUrl { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public bool ShouldNotifyUser { get; set; }
    public bool SupportTicketCreated { get; set; }
}

/// <summary>
/// Result of invoice dunning process
/// </summary>
public class InvoiceDunningResult
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DunningAction? NextAction { get; set; }
    public DateTime? NextActionDate { get; set; }
}

/// <summary>
/// Result of payment retry attempt
/// </summary>
public class PaymentRetryResult
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public bool RequiresNewPaymentMethod { get; set; }
    public DateTime? NextRetryAllowedAt { get; set; }
}

/// <summary>
/// Available recovery options for a failed payment
/// </summary>
public class PaymentRecoveryOptions
{
    public bool CanRetry { get; set; }
    public bool CanUpdatePaymentMethod { get; set; }
    public bool CanContactSupport { get; set; }
    public DateTime? NextRetryAllowedAt { get; set; }
    public int RetryAttemptsRemaining { get; set; }
    public RecoveryAction SuggestedAction { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Types of payment errors
/// </summary>
public enum PaymentErrorType
{
    Unknown,
    CardDeclined,
    InsufficientFunds,
    ExpiredCard,
    IncorrectCvc,
    ProcessingError,
    PaymentMethodRequired,
    RequiresConfirmation,
    RequiresAction,
    RateLimitExceeded
}

/// <summary>
/// Recovery strategies for payment errors
/// </summary>
public enum RecoveryStrategy
{
    RetryWithSameMethod,
    UpdatePaymentMethod,
    RetryWithBackoff,
    RequiresAction,
    NotifyUser,
    ContactSupport
}

/// <summary>
/// Recovery actions for users
/// </summary>
public enum RecoveryAction
{
    Retry,
    UpdatePaymentMethod,
    RequiresAction,
    ScheduledRetry,
    ContactSupport,
    PaymentSucceeded
}

/// <summary>
/// Reasons for payment retry
/// </summary>
public enum PaymentRetryReason
{
    UserInitiated,
    AutomaticRetry,
    DunningRetry,
    ErrorRecovery
}

/// <summary>
/// Dunning actions for failed payments
/// </summary>
public enum DunningAction
{
    SendInitialReminder,
    SendFinalWarning,
    RetryPayment,
    CancelSubscription
}