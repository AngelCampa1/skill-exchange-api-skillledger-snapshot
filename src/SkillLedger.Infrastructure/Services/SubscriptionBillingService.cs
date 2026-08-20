using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

public class SubscriptionBillingService : ISubscriptionBillingService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubscriptionBillingService> _logger;

    public SubscriptionBillingService(
        SkillLedgerDbContext context,
        ISubscriptionService subscriptionService,
        IPaymentService paymentService,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<SubscriptionBillingService> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _paymentService = paymentService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BillingProcessResult> ProcessDueRenewalsAsync()
    {
        _logger.LogInformation("Starting due subscription renewals process");

        var result = new BillingProcessResult();
        var now = DateTime.UtcNow;

        try
        {
            // Get subscriptions due for renewal
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var dueSubscriptions = await _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Include(us => us.User)
                .Include(us => us.PaymentMethod)
                .Include(us => us.Transactions)
                .AsSplitQuery()
                .Where(us => us.NextBillingDate <= now &&
                           (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial) &&
                           us.AutoRenew)
                .ToListAsync();

            result.TotalProcessed = dueSubscriptions.Count;

            foreach (var subscription in dueSubscriptions)
            {
                try
                {
                    _logger.LogInformation("Processing renewal for subscription {SubscriptionId}", subscription.Id);

                    // Calculate renewal amount
                    var amount = subscription.IsAnnual && subscription.SubscriptionTier.AnnualPrice.HasValue
                        ? subscription.SubscriptionTier.AnnualPrice.Value
                        : subscription.SubscriptionTier.Price;

                    // Process renewal payment
                    var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(
                        subscription.Id,
                        amount,
                        "USD",
                        $"Subscription renewal - {subscription.SubscriptionTier.Name}",
                        "127.0.0.1"); // System IP

                    if (paymentResult.Success)
                    {
                        // Update subscription
                        await _subscriptionService.RenewSubscriptionAsync(subscription.Id, "127.0.0.1");
                        result.SuccessfulRenewals++;
                        result.TotalRevenue += amount;
                        result.ProcessedSubscriptionIds.Add(subscription.Id);

                        _logger.LogInformation("Successfully renewed subscription {SubscriptionId}", subscription.Id);
                    }
                    else
                    {
                        // Handle payment failure
                        subscription.Status = SubscriptionStatus.PastDue;
                        subscription.NextRetryAt = DateTime.UtcNow.AddDays(1); // Retry tomorrow
                        subscription.RetryCount = 1;

                        await _context.SaveChangesAsync();

                        result.FailedRenewals++;
                        result.Errors.Add($"Payment failed for subscription {subscription.Id}: {paymentResult.ErrorMessage}");

                        // Send payment failure notification
                        await SendPaymentFailureNotificationAsync(subscription, paymentResult.ErrorMessage!);

                        await _auditLogService.LogEventAsync(
                            subscription.UserId,
                            "SUBSCRIPTION_RENEWAL_FAILED",
                            "127.0.0.1",
                            null,
                            false,
                            $"Renewal failed: {paymentResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing renewal for subscription {SubscriptionId}", subscription.Id);
                    result.FailedRenewals++;
                    result.Errors.Add($"Error processing subscription {subscription.Id}: {ex.Message}");
                }
            }

            _logger.LogInformation("Completed renewals process. Success: {Success}, Failed: {Failed}, Revenue: {Revenue}",
                result.SuccessfulRenewals, result.FailedRenewals, result.TotalRevenue);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in due renewals process");
            result.Errors.Add($"Process error: {ex.Message}");
            return result;
        }
    }

    public async Task<TrialConversionResult> ProcessExpiringTrialsAsync()
    {
        _logger.LogInformation("Processing expiring trials");

        var result = new TrialConversionResult();
        var now = DateTime.UtcNow;

        try
        {
            // Get trials expiring in the next 24 hours
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var expiringTrials = await _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Include(us => us.User)
                .AsSplitQuery()
                .Where(us => us.Status == SubscriptionStatus.Trial &&
                           us.TrialEndDate <= now &&
                           us.TrialEndDate > now.AddHours(-24)) // Within last 24 hours
                .ToListAsync();

            result.TrialsProcessed = expiringTrials.Count;

            foreach (var trial in expiringTrials)
            {
                try
                {
                    // Check if user has a payment method
                    var paymentMethods = await _paymentService.GetUserPaymentMethodsAsync(trial.UserId);
                    var defaultPaymentMethod = paymentMethods.FirstOrDefault(pm => pm.IsDefault);

                    if (defaultPaymentMethod != null)
                    {
                        // Attempt automatic conversion
                        var convertedSubscription = await _subscriptionService.ConvertTrialToPaidAsync(
                            trial.UserId,
                            defaultPaymentMethod.Id,
                            "127.0.0.1");

                        result.SuccessfulConversions++;
                        result.ConvertedSubscriptionIds.Add(trial.Id);

                        _logger.LogInformation("Successfully converted trial {TrialId} to paid subscription", trial.Id);

                        // Send conversion confirmation
                        await SendTrialConversionNotificationAsync(trial, true);
                    }
                    else
                    {
                        // No payment method available - cancel trial
                        trial.Status = SubscriptionStatus.Cancelled;
                        trial.EndDate = DateTime.UtcNow;
                        trial.CancelledAt = DateTime.UtcNow;
                        trial.CancellationReason = "Trial expired - no payment method available";

                        await _context.SaveChangesAsync();

                        result.FailedConversions++;
                        result.TrialsCancelled++;

                        // Send trial expiration notification
                        await SendTrialExpirationNotificationAsync(trial);

                        await _auditLogService.LogEventAsync(
                            trial.UserId,
                            "TRIAL_EXPIRED",
                            "127.0.0.1",
                            null,
                            false,
                            "Trial expired - no payment method on file");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing trial conversion for {TrialId}", trial.Id);
                    result.FailedConversions++;
                    result.Errors.Add($"Error converting trial {trial.Id}: {ex.Message}");
                }
            }

            _logger.LogInformation("Completed trial conversions. Converted: {Converted}, Failed: {Failed}, Cancelled: {Cancelled}",
                result.SuccessfulConversions, result.FailedConversions, result.TrialsCancelled);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in trial conversion process");
            result.Errors.Add($"Process error: {ex.Message}");
            return result;
        }
    }

    public async Task<RetryResult> ProcessFailedPaymentRetriesAsync(int maxRetries = 3)
    {
        _logger.LogInformation("Processing failed payment retries (max retries: {MaxRetries})", maxRetries);

        var result = new RetryResult();
        var now = DateTime.UtcNow;

        try
        {
            // Get subscriptions ready for retry
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var retryableSubscriptions = await _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Include(us => us.User)
                .Include(us => us.PaymentMethod)
                .AsSplitQuery()
                .Where(us => us.Status == SubscriptionStatus.PastDue &&
                           us.NextRetryAt <= now &&
                           us.RetryCount < maxRetries &&
                           us.PaymentMethodId.HasValue)
                .ToListAsync();

            result.RetriesAttempted = retryableSubscriptions.Count;

            foreach (var subscription in retryableSubscriptions)
            {
                try
                {
                    _logger.LogInformation("Retrying payment for subscription {SubscriptionId}, attempt {Attempt}",
                        subscription.Id, subscription.RetryCount + 1);

                    // Calculate retry amount
                    var amount = subscription.IsAnnual && subscription.SubscriptionTier.AnnualPrice.HasValue
                        ? subscription.SubscriptionTier.AnnualPrice.Value
                        : subscription.SubscriptionTier.Price;

                    // Process retry payment
                    var paymentResult = await _paymentService.ProcessSubscriptionPaymentAsync(
                        subscription.Id,
                        amount,
                        "USD",
                        $"Subscription retry payment - {subscription.SubscriptionTier.Name}",
                        "127.0.0.1");

                    if (paymentResult.Success)
                    {
                        // Payment successful - reactivate subscription
                        await _subscriptionService.RenewSubscriptionAsync(subscription.Id, "127.0.0.1");
                        result.SuccessfulRetries++;
                        result.RevenueRecovered += amount;

                        _logger.LogInformation("Retry successful for subscription {SubscriptionId}", subscription.Id);

                        // Send retry success notification
                        await SendRetrySuccessNotificationAsync(subscription);
                    }
                    else
                    {
                        // Payment failed again
                        subscription.RetryCount++;
                        subscription.NextRetryAt = CalculateNextRetryDate(subscription.RetryCount);

                        if (subscription.RetryCount >= maxRetries)
                        {
                            // Max retries reached - cancel subscription
                            subscription.Status = SubscriptionStatus.Cancelled;
                            subscription.EndDate = DateTime.UtcNow;
                            subscription.CancelledAt = DateTime.UtcNow;
                            subscription.CancellationReason = $"Payment failed after {maxRetries} retry attempts";

                            result.SubscriptionsCancelled++;

                            await SendMaxRetriesReachedNotificationAsync(subscription);
                        }

                        await _context.SaveChangesAsync();
                        result.FailedRetries++;

                        _logger.LogWarning("Retry failed for subscription {SubscriptionId}, attempt {Attempt}",
                            subscription.Id, subscription.RetryCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrying payment for subscription {SubscriptionId}", subscription.Id);
                    result.FailedRetries++;
                    result.Errors.Add($"Error retrying subscription {subscription.Id}: {ex.Message}");
                }
            }

            _logger.LogInformation("Completed retry process. Success: {Success}, Failed: {Failed}, Cancelled: {Cancelled}, Revenue: {Revenue}",
                result.SuccessfulRetries, result.FailedRetries, result.SubscriptionsCancelled, result.RevenueRecovered);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in retry process");
            result.Errors.Add($"Process error: {ex.Message}");
            return result;
        }
    }

    public async Task<CancellationResult> ProcessPastDueCancellationsAsync(int gracePeriodDays = 7)
    {
        _logger.LogInformation("Processing past due cancellations (grace period: {GracePeriodDays} days)", gracePeriodDays);

        var result = new CancellationResult();
        var cutoffDate = DateTime.UtcNow.AddDays(-gracePeriodDays);

        try
        {
            // Get subscriptions past due beyond grace period
            var pastDueSubscriptions = await _context.UserSubscriptions
                .Include(us => us.User)
                .Where(us => us.Status == SubscriptionStatus.PastDue &&
                           us.UpdatedAt <= cutoffDate)
                .ToListAsync();

            foreach (var subscription in pastDueSubscriptions)
            {
                try
                {
                    subscription.Status = SubscriptionStatus.Cancelled;
                    subscription.EndDate = DateTime.UtcNow;
                    subscription.CancelledAt = DateTime.UtcNow;
                    subscription.CancellationReason = $"Cancelled after {gracePeriodDays} days past due";

                    result.SubscriptionsCancelled++;
                    result.CancelledSubscriptionIds.Add(subscription.Id);

                    // Send cancellation notification
                    await SendPastDueCancellationNotificationAsync(subscription);

                    await _auditLogService.LogEventAsync(
                        subscription.UserId,
                        "SUBSCRIPTION_CANCELLED_PAST_DUE",
                        "127.0.0.1",
                        null,
                        false,
                        $"Cancelled after {gracePeriodDays} days past due");

                    _logger.LogInformation("Cancelled past due subscription {SubscriptionId}", subscription.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling past due subscription {SubscriptionId}", subscription.Id);
                    result.Errors.Add($"Error cancelling subscription {subscription.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed past due cancellations. Cancelled: {Cancelled}", result.SubscriptionsCancelled);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in past due cancellation process");
            result.Errors.Add($"Process error: {ex.Message}");
            return result;
        }
    }

    public async Task<ReminderResult> SendBillingRemindersAsync(int daysBefore = 3)
    {
        _logger.LogInformation("Sending billing reminders for renewals in {DaysBefore} days", daysBefore);

        var result = new ReminderResult();
        var reminderDate = DateTime.UtcNow.AddDays(daysBefore);

        try
        {
            // Get subscriptions due for renewal in specified days
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var upcomingRenewals = await _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Include(us => us.User)
                .AsSplitQuery()
                .Where(us => us.NextBillingDate <= reminderDate &&
                           us.NextBillingDate > DateTime.UtcNow &&
                           (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial))
                .ToListAsync();

            foreach (var subscription in upcomingRenewals)
            {
                try
                {
                    await SendBillingReminderNotificationAsync(subscription, daysBefore);
                    result.RemindersSent++;
                    result.UsersNotified++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reminder for subscription {SubscriptionId}", subscription.Id);
                    result.Errors.Add($"Error sending reminder for subscription {subscription.Id}: {ex.Message}");
                }
            }

            _logger.LogInformation("Completed billing reminders. Sent: {Sent}", result.RemindersSent);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in billing reminders process");
            result.Errors.Add($"Process error: {ex.Message}");
            return result;
        }
    }

    public async Task UpdateSubscriptionStatisticsAsync()
    {
        _logger.LogInformation("Updating subscription statistics");

        try
        {
            // This would typically update a statistics table or cache
            // For now, we'll just log current statistics
            var activeSubscriptions = await _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Where(us => us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial)
                .ToListAsync();

            var totalMRR = activeSubscriptions.Sum(us =>
                us.IsAnnual && us.SubscriptionTier.AnnualPrice.HasValue
                    ? us.SubscriptionTier.AnnualPrice.Value / 12
                    : us.SubscriptionTier.Price);

            _logger.LogInformation("Updated statistics: {Active} active subscriptions, ${MRR:F2} MRR",
                activeSubscriptions.Count, totalMRR);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription statistics");
        }
    }

    public async Task<ValidationResult> ValidateActiveSubscriptionsAsync()
    {
        _logger.LogInformation("Validating active subscriptions");

        var result = new ValidationResult();

        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var activeSubscriptions = await _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Include(us => us.User)
                .Include(us => us.PaymentMethod)
                .AsSplitQuery()
                .Where(us => us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial)
                .ToListAsync();

            result.TotalValidated = activeSubscriptions.Count;

            foreach (var subscription in activeSubscriptions)
            {
                var issues = new List<string>();

                // Validate payment method
                if (!subscription.PaymentMethodId.HasValue)
                {
                    issues.Add("No payment method assigned");
                }
                else if (subscription.PaymentMethod == null || !subscription.PaymentMethod.IsValid)
                {
                    issues.Add("Invalid or inactive payment method");
                }

                // Validate billing date
                if (subscription.NextBillingDate < DateTime.UtcNow.AddDays(-1))
                {
                    issues.Add("Next billing date is in the past");
                }

                // Validate trial dates
                if (subscription.Status == SubscriptionStatus.Trial)
                {
                    if (!subscription.TrialEndDate.HasValue)
                    {
                        issues.Add("Trial subscription has no end date");
                    }
                    else if (subscription.TrialEndDate.Value < DateTime.UtcNow)
                    {
                        issues.Add("Trial subscription should have ended");
                    }
                }

                if (issues.Any())
                {
                    result.InvalidSubscriptions++;
                    result.ValidationIssues.AddRange(issues.Select(issue => $"Subscription {subscription.Id}: {issue}"));
                    result.ProblematicSubscriptionIds.Add(subscription.Id);
                }
                else
                {
                    result.ValidSubscriptions++;
                }
            }

            _logger.LogInformation("Validation complete. Valid: {Valid}, Invalid: {Invalid}",
                result.ValidSubscriptions, result.InvalidSubscriptions);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in subscription validation");
            result.ValidationIssues.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    private DateTime CalculateNextRetryDate(int retryCount)
    {
        // Exponential backoff: 1 day, 2 days, 4 days, etc.
        var days = Math.Pow(2, retryCount - 1);
        return DateTime.UtcNow.AddDays(Math.Min(days, 7)); // Cap at 7 days
    }

    private async Task SendPaymentFailureNotificationAsync(UserSubscription subscription, string errorMessage)
    {
        try
        {
            // Send email notification about payment failure
            var subject = $"Payment Failed - {subscription.SubscriptionTier.Name} Subscription";
            var body = $@"
Dear {subscription.User.FirstName},

We attempted to renew your {subscription.SubscriptionTier.Name} subscription but encountered a payment issue.

Error: {errorMessage}

Please update your payment information to avoid service interruption.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(subscription.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment failure notification for subscription {SubscriptionId}", subscription.Id);
        }
    }

    private async Task SendTrialConversionNotificationAsync(UserSubscription trial, bool successful)
    {
        try
        {
            var subject = successful
                ? $"Trial Converted - {trial.SubscriptionTier.Name} Subscription"
                : $"Trial Expired - {trial.SubscriptionTier.Name} Subscription";

            var body = successful
                ? $@"
Dear {trial.User.FirstName},

Congratulations! Your trial has been successfully converted to a paid {trial.SubscriptionTier.Name} subscription.

Your subscription is now active and you can continue enjoying all the benefits.

Best regards,
SkillLedger Team"
                : $@"
Dear {trial.User.FirstName},

Your trial for the {trial.SubscriptionTier.Name} subscription has expired.

To continue enjoying our services, please add a payment method and subscribe again.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(trial.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending trial conversion notification for trial {TrialId}", trial.Id);
        }
    }

    private async Task SendTrialExpirationNotificationAsync(UserSubscription trial)
    {
        try
        {
            var subject = $"Trial Expired - {trial.SubscriptionTier.Name} Subscription";
            var body = $@"
Dear {trial.User.FirstName},

Your trial for the {trial.SubscriptionTier.Name} subscription has expired.

To continue enjoying our services, please add a payment method and subscribe again.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(trial.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending trial expiration notification for trial {TrialId}", trial.Id);
        }
    }

    private async Task SendRetrySuccessNotificationAsync(UserSubscription subscription)
    {
        try
        {
            var subject = $"Payment Successful - {subscription.SubscriptionTier.Name} Subscription";
            var body = $@"
Dear {subscription.User.FirstName},

Good news! Your payment retry was successful and your {subscription.SubscriptionTier.Name} subscription is now active.

Thank you for your continued support.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(subscription.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending retry success notification for subscription {SubscriptionId}", subscription.Id);
        }
    }

    private async Task SendMaxRetriesReachedNotificationAsync(UserSubscription subscription)
    {
        try
        {
            var subject = $"Subscription Cancelled - {subscription.SubscriptionTier.Name}";
            var body = $@"
Dear {subscription.User.FirstName},

We were unable to process your payment after multiple attempts and your {subscription.SubscriptionTier.Name} subscription has been cancelled.

You can reactivate your subscription at any time by updating your payment information.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(subscription.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending max retries notification for subscription {SubscriptionId}", subscription.Id);
        }
    }

    private async Task SendPastDueCancellationNotificationAsync(UserSubscription subscription)
    {
        try
        {
            var subject = $"Subscription Cancelled - {subscription.SubscriptionTier.Name}";
            var body = $@"
Dear {subscription.User.FirstName},

Your {subscription.SubscriptionTier.Name} subscription has been cancelled due to prolonged payment issues.

You can reactivate your subscription at any time by updating your payment information.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(subscription.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending past due cancellation notification for subscription {SubscriptionId}", subscription.Id);
        }
    }

    private async Task SendBillingReminderNotificationAsync(UserSubscription subscription, int daysBefore)
    {
        try
        {
            var amount = subscription.IsAnnual && subscription.SubscriptionTier.AnnualPrice.HasValue
                ? subscription.SubscriptionTier.AnnualPrice.Value
                : subscription.SubscriptionTier.Price;

            var subject = $"Upcoming Subscription Renewal - {subscription.SubscriptionTier.Name}";
            var body = $@"
Dear {subscription.User.FirstName},

This is a friendly reminder that your {subscription.SubscriptionTier.Name} subscription will renew in {daysBefore} days.

Amount: ${amount:F2}
Renewal Date: {subscription.NextBillingDate:d}

Please ensure your payment method is up to date.

Best regards,
SkillLedger Team";

            await _emailService.SendEmailAsync(subscription.User.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending billing reminder for subscription {SubscriptionId}", subscription.Id);
        }
    }
}