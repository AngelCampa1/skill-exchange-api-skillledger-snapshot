using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock payment service for testing.
/// This mocks the EXTERNAL Stripe payment service (OK to mock).
/// Allows configuring success/failure scenarios and tracking processed payments.
/// </summary>
public class MockPaymentService : IPaymentService
{
    private bool _shouldSucceed = true;
    private string _errorMessage = "Payment declined";
    private readonly List<ProcessedPayment> _processedPayments = new();
    private readonly List<ProcessedRefund> _processedRefunds = new();
    private readonly Dictionary<Guid, List<PaymentMethod>> _userPaymentMethods = new();

    /// <summary>
    /// List of all payments processed through this mock
    /// </summary>
    public IReadOnlyList<ProcessedPayment> ProcessedPayments => _processedPayments.AsReadOnly();

    /// <summary>
    /// List of all refunds processed through this mock
    /// </summary>
    public IReadOnlyList<ProcessedRefund> ProcessedRefunds => _processedRefunds.AsReadOnly();

    /// <summary>
    /// Configure the mock to succeed on payment requests
    /// </summary>
    public void SetupSuccess()
    {
        _shouldSucceed = true;
    }

    /// <summary>
    /// Configure the mock to fail on payment requests with specific error
    /// </summary>
    public void SetupFailure(string errorMessage = "Payment declined")
    {
        _shouldSucceed = false;
        _errorMessage = errorMessage;
    }

    /// <summary>
    /// Add a payment method for a user (for GetUserPaymentMethodsAsync)
    /// </summary>
    public void AddPaymentMethodForUser(Guid userId, PaymentMethod paymentMethod)
    {
        if (!_userPaymentMethods.ContainsKey(userId))
        {
            _userPaymentMethods[userId] = new List<PaymentMethod>();
        }
        _userPaymentMethods[userId].Add(paymentMethod);
    }

    /// <summary>
    /// Clear all state
    /// </summary>
    public void Reset()
    {
        _shouldSucceed = true;
        _errorMessage = "Payment declined";
        _processedPayments.Clear();
        _processedRefunds.Clear();
        _userPaymentMethods.Clear();
    }

    public Task<PaymentMethod> CreatePaymentMethodAsync(
        Guid userId,
        string provider,
        string paymentMethodToken,
        bool isDefault = false,
        string? createdFromIP = null)
    {
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            Token = $"pm_{Guid.NewGuid():N}",
            Type = "card",
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryDate = "12/2030",
            IsDefault = isDefault,
            IsValid = true,
            CreatedAt = DateTime.UtcNow
        };

        AddPaymentMethodForUser(userId, paymentMethod);
        return Task.FromResult(paymentMethod);
    }

    public Task<PaymentMethod?> GetPaymentMethodAsync(Guid paymentMethodId, Guid userId)
    {
        if (_userPaymentMethods.TryGetValue(userId, out var methods))
        {
            return Task.FromResult(methods.FirstOrDefault(m => m.Id == paymentMethodId));
        }
        return Task.FromResult<PaymentMethod?>(null);
    }

    public Task<List<PaymentMethod>> GetUserPaymentMethodsAsync(Guid userId)
    {
        if (_userPaymentMethods.TryGetValue(userId, out var methods))
        {
            return Task.FromResult(methods);
        }
        return Task.FromResult(new List<PaymentMethod>());
    }

    public Task<PaymentMethod> SavePaymentMethodFromWebhookAsync(PaymentMethod paymentMethod)
    {
        AddPaymentMethodForUser(paymentMethod.UserId, paymentMethod);
        return Task.FromResult(paymentMethod);
    }

    public Task<PaymentMethod> SetDefaultPaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId,
        string? createdFromIP = null)
    {
        if (_userPaymentMethods.TryGetValue(userId, out var methods))
        {
            foreach (var method in methods)
            {
                method.IsDefault = method.Id == paymentMethodId;
            }
            var defaultMethod = methods.FirstOrDefault(m => m.Id == paymentMethodId);
            if (defaultMethod != null)
            {
                return Task.FromResult(defaultMethod);
            }
        }
        throw new InvalidOperationException("Payment method not found");
    }

    public Task<bool> RemovePaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId,
        string? createdFromIP = null)
    {
        if (_userPaymentMethods.TryGetValue(userId, out var methods))
        {
            var removed = methods.RemoveAll(m => m.Id == paymentMethodId) > 0;
            return Task.FromResult(removed);
        }
        return Task.FromResult(false);
    }

    public Task<PaymentResult> ProcessSubscriptionPaymentAsync(
        Guid subscriptionId,
        decimal amount,
        string currency = "USD",
        string? description = null,
        string? createdFromIP = null)
    {
        _processedPayments.Add(new ProcessedPayment
        {
            SubscriptionId = subscriptionId,
            Amount = amount,
            Currency = currency,
            Description = description,
            ProcessedAt = DateTime.UtcNow,
            Success = _shouldSucceed
        });

        if (_shouldSucceed)
        {
            return Task.FromResult(new PaymentResult
            {
                Success = true,
                ExternalTransactionId = $"txn_{Guid.NewGuid():N}",
                Status = TransactionStatus.Completed,
                Transaction = new SubscriptionTransaction
                {
                    Id = Guid.NewGuid(),
                    Amount = amount,
                    Currency = currency,
                    Status = TransactionStatus.Completed,
                    Type = SubscriptionTransactionType.Renewal,
                    Description = description ?? "Subscription payment",
                    CreatedAt = DateTime.UtcNow
                }
            });
        }

        return Task.FromResult(new PaymentResult
        {
            Success = false,
            ErrorMessage = _errorMessage,
            Status = TransactionStatus.Failed
        });
    }

    public Task<PaymentResult> ProcessOneTimePaymentAsync(
        Guid userId,
        Guid paymentMethodId,
        decimal amount,
        string currency = "USD",
        string? description = null,
        string? createdFromIP = null)
    {
        _processedPayments.Add(new ProcessedPayment
        {
            UserId = userId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            Currency = currency,
            Description = description,
            ProcessedAt = DateTime.UtcNow,
            Success = _shouldSucceed
        });

        if (_shouldSucceed)
        {
            return Task.FromResult(new PaymentResult
            {
                Success = true,
                ExternalTransactionId = $"txn_{Guid.NewGuid():N}",
                Status = TransactionStatus.Completed
            });
        }

        return Task.FromResult(new PaymentResult
        {
            Success = false,
            ErrorMessage = _errorMessage,
            Status = TransactionStatus.Failed
        });
    }

    public Task<RefundResult> RefundPaymentAsync(
        Guid transactionId,
        Guid? requestingUserId = null,
        decimal? amount = null,
        string? reason = null,
        string? createdFromIP = null)
    {
        _processedRefunds.Add(new ProcessedRefund
        {
            TransactionId = transactionId,
            Amount = amount,
            Reason = reason,
            ProcessedAt = DateTime.UtcNow,
            Success = _shouldSucceed
        });

        if (_shouldSucceed)
        {
            return Task.FromResult(new RefundResult
            {
                Success = true,
                ExternalRefundId = $"ref_{Guid.NewGuid():N}",
                Status = TransactionStatus.Completed,
                RefundedAmount = amount ?? 0
            });
        }

        return Task.FromResult(new RefundResult
        {
            Success = false,
            ErrorMessage = _errorMessage,
            Status = TransactionStatus.Failed
        });
    }

    public Task<PaymentValidationResult> ValidatePaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId)
    {
        return Task.FromResult(new PaymentValidationResult
        {
            IsValid = true,
            IsExpired = false,
            ExpiryDate = DateTime.UtcNow.AddYears(3)
        });
    }

    public Task<string> CreateExternalCustomerAsync(
        Guid userId,
        string email,
        string name)
    {
        return Task.FromResult($"cus_{Guid.NewGuid():N}");
    }

    public Task<bool> UpdateExternalCustomerAsync(
        Guid userId,
        string email,
        string name)
    {
        return Task.FromResult(true);
    }

    public Task<PaymentMethodDetails> GetPaymentMethodDetailsAsync(
        string paymentMethodToken,
        string provider)
    {
        return Task.FromResult(new PaymentMethodDetails
        {
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryMonth = "12",
            ExpiryYear = "2030",
            IsValid = true,
            ExpiryDate = new DateTime(2030, 12, 31)
        });
    }

    public Task<WebhookResult> ProcessWebhookAsync(
        string provider,
        string eventType,
        string eventData)
    {
        return Task.FromResult(new WebhookResult
        {
            Success = true,
            ProcessedEvents = new List<string> { eventType }
        });
    }
}

/// <summary>
/// Record of a processed payment for test assertions
/// </summary>
public class ProcessedPayment
{
    public Guid? SubscriptionId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Description { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Record of a processed refund for test assertions
/// </summary>
public class ProcessedRefund
{
    public Guid TransactionId { get; set; }
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool Success { get; set; }
}
