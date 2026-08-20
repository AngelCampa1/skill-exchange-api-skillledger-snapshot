namespace SkillLedger.Infrastructure.Configuration;

/// <summary>
/// Configuration for payment retry and error handling policies
/// </summary>
public class PaymentRetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts for failed payments
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum number of dunning attempts before subscription cancellation
    /// </summary>
    public int MaxDunningAttempts { get; set; } = 4;

    /// <summary>
    /// Time intervals for dunning attempts (in days from initial failure)
    /// </summary>
    public int[] DunningIntervals { get; set; } = { 3, 5, 7, 10 };

    /// <summary>
    /// Whether to enable automatic payment retries
    /// </summary>
    public bool EnableAutomaticRetries { get; set; } = true;

    /// <summary>
    /// Whether to enable dunning workflow for failed invoices
    /// </summary>
    public bool EnableDunningWorkflow { get; set; } = true;

    /// <summary>
    /// Maximum amount for automatic retry attempts (in cents)
    /// </summary>
    public long MaxRetryAmount { get; set; } = 100000; // $1000

    /// <summary>
    /// Base URL for payment method update flows
    /// </summary>
    public string PaymentMethodUpdateBaseUrl { get; set; } = "https://localhost:3030";

    /// <summary>
    /// Support email for escalated payment issues
    /// </summary>
    public string SupportEmail { get; set; } = "angel.campa@skillledger.app";
}