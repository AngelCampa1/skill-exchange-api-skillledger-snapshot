namespace SkillLedger.Core.Interfaces;

public interface ISubscriptionBillingService
{
    /// <summary>
    /// Processes all due subscription renewals (typically called by background job)
    /// </summary>
    /// <returns>Billing processing results</returns>
    Task<BillingProcessResult> ProcessDueRenewalsAsync();

    /// <summary>
    /// Processes expiring trials and converts them to paid subscriptions
    /// </summary>
    /// <returns>Trial conversion results</returns>
    Task<TrialConversionResult> ProcessExpiringTrialsAsync();

    /// <summary>
    /// Handles failed payment retries
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <returns>Retry processing results</returns>
    Task<RetryResult> ProcessFailedPaymentRetriesAsync(int maxRetries = 3);

    /// <summary>
    /// Cancels subscriptions that are past due beyond grace period
    /// </summary>
    /// <param name="gracePeriodDays">Grace period in days before cancellation</param>
    /// <returns>Cancellation results</returns>
    Task<CancellationResult> ProcessPastDueCancellationsAsync(int gracePeriodDays = 7);

    /// <summary>
    /// Generates and sends billing reminders for upcoming renewals
    /// </summary>
    /// <param name="daysBefore">Number of days before renewal to send reminder</param>
    /// <returns>Reminder results</returns>
    Task<ReminderResult> SendBillingRemindersAsync(int daysBefore = 3);

    /// <summary>
    /// Updates subscription statistics for reporting
    /// </summary>
    /// <returns>Task completion</returns>
    Task UpdateSubscriptionStatisticsAsync();

    /// <summary>
    /// Validates all active subscriptions for compliance and billing status
    /// </summary>
    /// <returns>Validation results</returns>
    Task<ValidationResult> ValidateActiveSubscriptionsAsync();
}

/// <summary>
/// Results from billing process
/// </summary>
public class BillingProcessResult
{
    public int TotalProcessed { get; set; }
    public int SuccessfulRenewals { get; set; }
    public int FailedRenewals { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> ProcessedSubscriptionIds { get; set; } = new();
}

/// <summary>
/// Results from trial conversion process
/// </summary>
public class TrialConversionResult
{
    public int TrialsProcessed { get; set; }
    public int SuccessfulConversions { get; set; }
    public int FailedConversions { get; set; }
    public int TrialsCancelled { get; set; }
    public List<Guid> ConvertedSubscriptionIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Results from payment retry process
/// </summary>
public class RetryResult
{
    public int RetriesAttempted { get; set; }
    public int SuccessfulRetries { get; set; }
    public int FailedRetries { get; set; }
    public int SubscriptionsCancelled { get; set; }
    public decimal RevenueRecovered { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Results from cancellation process
/// </summary>
public class CancellationResult
{
    public int SubscriptionsCancelled { get; set; }
    public int UsersNotified { get; set; }
    public List<Guid> CancelledSubscriptionIds { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Results from reminder process
/// </summary>
public class ReminderResult
{
    public int RemindersSent { get; set; }
    public int UsersNotified { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Results from subscription validation
/// </summary>
public class ValidationResult
{
    public int TotalValidated { get; set; }
    public int ValidSubscriptions { get; set; }
    public int InvalidSubscriptions { get; set; }
    public List<string> ValidationIssues { get; set; } = new();
    public List<Guid> ProblematicSubscriptionIds { get; set; } = new();
}