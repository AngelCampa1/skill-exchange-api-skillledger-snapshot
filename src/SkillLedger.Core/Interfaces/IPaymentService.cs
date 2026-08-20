using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Creates a new payment method for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="provider">Payment provider (e.g., 'stripe')</param>
    /// <param name="paymentMethodToken">Token from payment provider</param>
    /// <param name="isDefault">Whether this should be the default payment method</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Created payment method</returns>
    Task<PaymentMethod> CreatePaymentMethodAsync(
        Guid userId,
        string provider,
        string paymentMethodToken,
        bool isDefault = false,
        string? createdFromIP = null);

    /// <summary>
    /// Gets a payment method by ID
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="userId">User ID (for security)</param>
    /// <returns>Payment method or null</returns>
    Task<PaymentMethod?> GetPaymentMethodAsync(Guid paymentMethodId, Guid userId);

    /// <summary>
    /// Gets all payment methods for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User's payment methods</returns>
    Task<List<PaymentMethod>> GetUserPaymentMethodsAsync(Guid userId);

    /// <summary>
    /// Saves a payment method synced from a webhook (e.g., after Stripe Checkout setup)
    /// </summary>
    /// <param name="paymentMethod">Payment method to save</param>
    /// <returns>Saved payment method</returns>
    Task<PaymentMethod> SavePaymentMethodFromWebhookAsync(PaymentMethod paymentMethod);

    /// <summary>
    /// Sets a payment method as the default for a user
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Updated payment method</returns>
    Task<PaymentMethod> SetDefaultPaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId,
        string? createdFromIP = null);

    /// <summary>
    /// Removes a payment method
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>True if successfully removed</returns>
    Task<bool> RemovePaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId,
        string? createdFromIP = null);

    /// <summary>
    /// Processes a payment for a subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="amount">Amount to charge</param>
    /// <param name="currency">Currency code</param>
    /// <param name="description">Payment description</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Payment transaction result</returns>
    Task<PaymentResult> ProcessSubscriptionPaymentAsync(
        Guid subscriptionId,
        decimal amount,
        string currency = "USD",
        string? description = null,
        string? createdFromIP = null);

    /// <summary>
    /// Processes a one-time payment
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="amount">Amount to charge</param>
    /// <param name="currency">Currency code</param>
    /// <param name="description">Payment description</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Payment transaction result</returns>
    Task<PaymentResult> ProcessOneTimePaymentAsync(
        Guid userId,
        Guid paymentMethodId,
        decimal amount,
        string currency = "USD",
        string? description = null,
        string? createdFromIP = null);

    /// <summary>
    /// Refunds a payment transaction
    /// </summary>
    /// <param name="transactionId">Transaction ID to refund</param>
    /// <param name="requestingUserId">Authenticated user requesting the refund, or null for trusted system/admin flows</param>
    /// <param name="amount">Amount to refund (partial refund)</param>
    /// <param name="reason">Refund reason</param>
    /// <param name="createdFromIP">IP address of the request</param>
    /// <returns>Refund result</returns>
    Task<RefundResult> RefundPaymentAsync(
        Guid transactionId,
        Guid? requestingUserId = null,
        decimal? amount = null,
        string? reason = null,
        string? createdFromIP = null);

    /// <summary>
    /// Validates a payment method (checks if it's still valid)
    /// </summary>
    /// <param name="paymentMethodId">Payment method ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Validation result</returns>
    Task<PaymentValidationResult> ValidatePaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId);

    /// <summary>
    /// Creates a customer account with the payment provider
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <returns>External customer ID</returns>
    Task<string> CreateExternalCustomerAsync(
        Guid userId,
        string email,
        string name);

    /// <summary>
    /// Updates external customer information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <returns>True if successfully updated</returns>
    Task<bool> UpdateExternalCustomerAsync(
        Guid userId,
        string email,
        string name);

    /// <summary>
    /// Gets payment method details from external provider
    /// </summary>
    /// <param name="paymentMethodToken">Payment method token</param>
    /// <param name="provider">Payment provider</param>
    /// <returns>Payment method details</returns>
    Task<PaymentMethodDetails> GetPaymentMethodDetailsAsync(
        string paymentMethodToken,
        string provider);

    /// <summary>
    /// Handles webhook events from payment provider
    /// </summary>
    /// <param name="provider">Payment provider</param>
    /// <param name="eventType">Event type</param>
    /// <param name="eventData">Event data</param>
    /// <returns>Webhook processing result</returns>
    Task<WebhookResult> ProcessWebhookAsync(
        string provider,
        string eventType,
        string eventData);
}

/// <summary>
/// Result of a payment transaction
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public SubscriptionTransaction? Transaction { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public TransactionStatus Status { get; set; }
    public bool RequiresAction { get; set; }
    public string? ClientSecret { get; set; }
    public string? NextActionUrl { get; set; }
}

/// <summary>
/// Result of a refund transaction
/// </summary>
public class RefundResult
{
    public bool Success { get; set; }
    public SubscriptionTransaction? RefundTransaction { get; set; }
    public string? ExternalRefundId { get; set; }
    public string? ErrorMessage { get; set; }
    public TransactionStatus Status { get; set; }
    public decimal RefundedAmount { get; set; }
}

/// <summary>
/// Result of payment method validation
/// </summary>
public class PaymentValidationResult
{
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Payment method details from external provider
/// </summary>
public class PaymentMethodDetails
{
    public string Last4Digits { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? CardholderName { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Result of webhook processing
/// </summary>
public class WebhookResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ProcessedEvents { get; set; } = new();
    public List<string> FailedEvents { get; set; } = new();
}
