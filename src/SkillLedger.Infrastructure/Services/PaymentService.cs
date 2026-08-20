using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using Stripe;
// BUG-CRIT-002 FIX: Use alias to avoid namespace collision with Stripe.PaymentMethod
using DbPaymentMethod = SkillLedger.Core.Entities.PaymentMethod;

namespace SkillLedger.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedLockService _lockService;
    private readonly bool _stripeEnabled;
    private readonly bool _isTestMode;

    public PaymentService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        ILogger<PaymentService> logger,
        IConfiguration configuration,
        IDistributedLockService lockService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
        _configuration = configuration;
        _lockService = lockService;

        // BUG-CRIT-002 FIX: Initialize Stripe configuration
        _stripeEnabled = _configuration.GetValue<bool>("Stripe:IsEnabled");
        _isTestMode = _configuration.GetValue<bool>("Stripe:IsTestMode", true);

        var secretKey = _configuration["Stripe:SecretKey"];
        // BE-LOW-004 FIX: Validate Stripe key format using regex
        // Stripe secret keys have format: sk_test_... or sk_live_... (minimum ~50 chars)
        var isValidStripeKeyFormat = !string.IsNullOrEmpty(secretKey) &&
            System.Text.RegularExpressions.Regex.IsMatch(secretKey, @"^sk_(test|live)_[a-zA-Z0-9]{24,}$");

        if (_stripeEnabled && isValidStripeKeyFormat)
        {
            StripeConfiguration.ApiKey = secretKey;
            _logger.LogInformation("Stripe payment provider initialized (Test Mode: {IsTestMode})", _isTestMode);
        }
        else if (_stripeEnabled && !string.IsNullOrEmpty(secretKey) && !secretKey.StartsWith("REPLACE_WITH"))
        {
            // Key exists but doesn't match expected format - might be valid but warn
            _logger.LogWarning("Stripe SecretKey may be invalid format. Expected sk_test_* or sk_live_*");
            StripeConfiguration.ApiKey = secretKey; // Still try to use it
        }
        else if (_stripeEnabled)
        {
            _logger.LogWarning("Stripe is enabled but SecretKey is not configured. Payment processing will fail.");
        }
    }

    public async Task<DbPaymentMethod> CreatePaymentMethodAsync(
        Guid userId,
        string provider,
        string paymentMethodToken,
        bool isDefault = false,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Creating payment method for user {UserId}, provider {Provider}", userId, provider);

        try
        {
            // Get payment method details from external provider
            var details = await GetPaymentMethodDetailsAsync(paymentMethodToken, provider);

            // If setting as default, unset other default payment methods
            if (isDefault)
            {
                var existingDefaults = await _context.PaymentMethods
                    .Where(pm => pm.UserId == userId && pm.IsDefault)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            var paymentMethod = new DbPaymentMethod
            {
                UserId = userId,
                Provider = provider,
                Type = details.IsValid ? "card" : "unknown", // Would be determined by provider response
                Token = paymentMethodToken,
                Last4Digits = details.Last4Digits,
                Brand = details.Brand,
                ExpiryDate = $"{details.ExpiryMonth}/{details.ExpiryYear}",
                CardholderName = details.CardholderName,
                BillingCountry = details.BillingCountry,
                BillingPostalCode = details.BillingPostalCode,
                IsDefault = isDefault,
                IsValid = details.IsValid,
                ExpiresAt = details.ExpiryDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "PAYMENT_METHOD_CREATED",
                createdFromIP ?? "Unknown",
                null,
                true,
                $"Payment method created: {provider}, ****{details.Last4Digits}");

            return paymentMethod;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method for user {UserId}", userId);
            // BE-LOW-003 FIX: Mask exception details in audit logs to prevent information disclosure
            await _auditLogService.LogEventAsync(
                userId,
                "PAYMENT_METHOD_CREATE_FAILED",
                createdFromIP ?? "Unknown",
                null,
                false,
                "Payment method creation failed. Check logs for details.");
            throw;
        }
    }

    public async Task<DbPaymentMethod?> GetPaymentMethodAsync(Guid paymentMethodId, Guid userId)
    {
        return await _context.PaymentMethods
            .Where(pm => pm.Id == paymentMethodId && pm.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<DbPaymentMethod>> GetUserPaymentMethodsAsync(Guid userId)
    {
        return await _context.PaymentMethods
            .Where(pm => pm.UserId == userId)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt)
            .ToListAsync();
    }

    public async Task<DbPaymentMethod> SavePaymentMethodFromWebhookAsync(DbPaymentMethod paymentMethod)
    {
        // Check if a payment method with this Stripe token already exists
        var existing = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Token == paymentMethod.Token);

        if (existing != null)
        {
            // Update existing payment method
            existing.Last4Digits = paymentMethod.Last4Digits;
            existing.Brand = paymentMethod.Brand;
            existing.ExpiryDate = paymentMethod.ExpiryDate;
            existing.CardholderName = paymentMethod.CardholderName;
            existing.BillingCountry = paymentMethod.BillingCountry;
            existing.BillingPostalCode = paymentMethod.BillingPostalCode;
            existing.IsValid = paymentMethod.IsValid;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        // Add new payment method
        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(
            paymentMethod.UserId,
            "PAYMENT_METHOD_SYNCED_FROM_WEBHOOK",
            "Webhook",
            null,
            true,
            $"Payment method synced: ****{paymentMethod.Last4Digits} ({paymentMethod.Brand})");

        return paymentMethod;
    }

    public async Task<DbPaymentMethod> SetDefaultPaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId,
        string? createdFromIP = null)
    {
        var paymentMethod = await GetPaymentMethodAsync(paymentMethodId, userId);
        if (paymentMethod == null)
            throw new ArgumentException("Payment method not found", nameof(paymentMethodId));

        // Unset other default payment methods
        var existingDefaults = await _context.PaymentMethods
            .Where(pm => pm.UserId == userId && pm.IsDefault)
            .ToListAsync();

        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Set new default
        paymentMethod.IsDefault = true;
        paymentMethod.UpdatedAt = DateTime.UtcNow;
        paymentMethod.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(
            userId,
            "PAYMENT_METHOD_SET_DEFAULT",
            createdFromIP ?? "Unknown",
            null,
            true,
            $"Set default payment method: ****{paymentMethod.Last4Digits}");

        return paymentMethod;
    }

    public async Task<bool> RemovePaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId,
        string? createdFromIP = null)
    {
        var paymentMethod = await GetPaymentMethodAsync(paymentMethodId, userId);
        if (paymentMethod == null)
            return false;

        // Check if this payment method is used by any active subscriptions
        var activeSubscriptionCount = await _context.UserSubscriptions
            .CountAsync(us => us.UserId == userId &&
                             us.PaymentMethodId == paymentMethodId &&
                             (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial));

        if (activeSubscriptionCount > 0)
            throw new InvalidOperationException("Cannot remove payment method used by active subscriptions");

        _context.PaymentMethods.Remove(paymentMethod);
        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(
            userId,
            "PAYMENT_METHOD_REMOVED",
            createdFromIP ?? "Unknown",
            null,
            true,
            $"Removed payment method: ****{paymentMethod.Last4Digits}");

        return true;
    }

    public async Task<PaymentResult> ProcessSubscriptionPaymentAsync(
        Guid subscriptionId,
        decimal amount,
        string currency = "USD",
        string? description = null,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Processing subscription payment {SubscriptionId}, amount {Amount}", subscriptionId, amount);

        // BUG-CRIT-007 FIX: Add distributed locking to prevent concurrent payment processing
        // Lock key is unique per subscription to prevent double-charging
        var lockKey = $"payment:subscription:{subscriptionId}";

        await using var distributedLock = await _lockService.AcquireLockAsync(
            lockKey,
            expirationTime: TimeSpan.FromMinutes(2),  // Maximum time for payment processing
            waitTime: TimeSpan.FromSeconds(30),       // Wait up to 30 seconds for lock
            retryTime: TimeSpan.FromMilliseconds(500) // Retry every 500ms
        );

        if (!distributedLock.IsAcquired)
        {
            _logger.LogWarning("Failed to acquire lock for subscription payment {SubscriptionId}. Payment may already be processing.", subscriptionId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = "Payment is already being processed. Please wait.",
                Status = TransactionStatus.Failed
            };
        }

        _logger.LogInformation("Acquired distributed lock for subscription payment {SubscriptionId}", subscriptionId);

        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var subscription = await _context.UserSubscriptions
                .Include(us => us.User)
                .Include(us => us.PaymentMethod)
                .AsSplitQuery()
                .FirstOrDefaultAsync(us => us.Id == subscriptionId);

            if (subscription == null)
                throw new ArgumentException("Subscription not found", nameof(subscriptionId));

            if (subscription.PaymentMethod == null)
                throw new InvalidOperationException("No payment method associated with subscription");

            // BUG-CRIT-002 FIX: Real Stripe payment processing
            string externalTransactionId;
            bool success;
            string? errorMessage = null;
            TransactionStatus status;

            if (_stripeEnabled)
            {
                try
                {
                    // Process payment through Stripe
                    var paymentIntentService = new PaymentIntentService();
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = (long)(amount * 100), // Stripe uses cents
                        Currency = currency.ToLower(),
                        Description = description ?? $"Subscription payment for {subscription.User.Email}",
                        PaymentMethod = subscription.PaymentMethod.Token, // BUG-CRIT-002 FIX: Use Token property
                        Customer = subscription.User.ExternalCustomerId,
                        Confirm = true,
                        OffSession = true, // This is for recurring payments
                        Metadata = new Dictionary<string, string>
                        {
                            { "subscription_id", subscriptionId.ToString() },
                            { "user_id", subscription.UserId.ToString() }
                        }
                    };

                    var paymentIntent = await paymentIntentService.CreateAsync(options);
                    externalTransactionId = paymentIntent.Id;
                    success = paymentIntent.Status == "succeeded";
                    status = success ? TransactionStatus.Completed : TransactionStatus.Failed;
                    errorMessage = success ? null : paymentIntent.CancellationReason ?? "Payment failed";

                    _logger.LogInformation(
                        "Stripe payment {Status} for subscription {SubscriptionId}, PaymentIntent: {PaymentIntentId}",
                        paymentIntent.Status, subscriptionId, paymentIntent.Id);
                }
                catch (StripeException stripeEx)
                {
                    _logger.LogError(stripeEx, "Stripe error processing payment for subscription {SubscriptionId}", subscriptionId);
                    externalTransactionId = $"stripe_error_{Guid.NewGuid():N}";
                    success = false;
                    status = TransactionStatus.Failed;
                    errorMessage = stripeEx.Message;
                }
            }
            else
            {
                // Stripe not enabled - fail with clear message
                _logger.LogWarning("Payment attempted but Stripe is not enabled (Subscription: {SubscriptionId})", subscriptionId);
                externalTransactionId = $"no_provider_{Guid.NewGuid():N}";
                success = false;
                status = TransactionStatus.Failed;
                errorMessage = "Payment provider not configured. Please contact support.";
            }

            // Create transaction record
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = subscriptionId,
                UserId = subscription.UserId,
                Type = SubscriptionTransactionType.Renewal,
                Amount = amount,
                Currency = currency,
                PaymentMethodId = subscription.PaymentMethodId,
                ExternalTransactionId = externalTransactionId,
                Status = status,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                CompletedAt = success ? DateTime.UtcNow : null,
                FailedAt = !success ? DateTime.UtcNow : null,
                CreatedFromIP = createdFromIP,
                UserAgent = null
            };

            _context.SubscriptionTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                subscription.UserId,
                success ? "PAYMENT_PROCESSED" : "PAYMENT_FAILED",
                createdFromIP ?? "Unknown",
                null,
                success,
                $"{description}, Amount: {amount} {currency}, Transaction: {externalTransactionId}");

            return new PaymentResult
            {
                Success = success,
                Transaction = transaction,
                ExternalTransactionId = externalTransactionId,
                ErrorMessage = errorMessage,
                Status = status,
                RequiresAction = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription payment {SubscriptionId}", subscriptionId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Status = TransactionStatus.Failed
            };
        }
    }

    public async Task<PaymentResult> ProcessOneTimePaymentAsync(
        Guid userId,
        Guid paymentMethodId,
        decimal amount,
        string currency = "USD",
        string? description = null,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Processing one-time payment for user {UserId}, amount {Amount}", userId, amount);

        try
        {
            var paymentMethod = await GetPaymentMethodAsync(paymentMethodId, userId);
            if (paymentMethod == null)
                throw new ArgumentException("Payment method not found", nameof(paymentMethodId));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found", nameof(userId));

            // BUG-CRIT-002 FIX: Real Stripe payment processing for one-time payments
            string externalTransactionId;
            bool success;
            string? errorMessage = null;
            TransactionStatus status;

            if (_stripeEnabled)
            {
                try
                {
                    // Process payment through Stripe
                    var paymentIntentService = new PaymentIntentService();
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = (long)(amount * 100), // Stripe uses cents
                        Currency = currency.ToLower(),
                        Description = description ?? $"One-time payment for {user.Email}",
                        PaymentMethod = paymentMethod.Token,
                        Customer = user.ExternalCustomerId,
                        Confirm = true,
                        Metadata = new Dictionary<string, string>
                        {
                            { "user_id", userId.ToString() },
                            { "payment_type", "one_time" }
                        }
                    };

                    var paymentIntent = await paymentIntentService.CreateAsync(options);
                    externalTransactionId = paymentIntent.Id;
                    success = paymentIntent.Status == "succeeded";
                    status = success ? TransactionStatus.Completed : TransactionStatus.Failed;
                    errorMessage = success ? null : paymentIntent.CancellationReason ?? "Payment failed";

                    _logger.LogInformation(
                        "Stripe one-time payment {Status} for user {UserId}, PaymentIntent: {PaymentIntentId}",
                        paymentIntent.Status, userId, paymentIntent.Id);
                }
                catch (StripeException stripeEx)
                {
                    _logger.LogError(stripeEx, "Stripe error processing one-time payment for user {UserId}", userId);
                    externalTransactionId = $"stripe_error_{Guid.NewGuid():N}";
                    success = false;
                    status = TransactionStatus.Failed;
                    errorMessage = stripeEx.Message;
                }
            }
            else
            {
                // Stripe not enabled - fail with clear message
                _logger.LogWarning("One-time payment attempted but Stripe is not enabled (User: {UserId})", userId);
                externalTransactionId = $"no_provider_{Guid.NewGuid():N}";
                success = false;
                status = TransactionStatus.Failed;
                errorMessage = "Payment provider not configured. Please contact support.";
            }

            // Create transaction record (without subscription)
            var transaction = new SubscriptionTransaction
            {
                SubscriptionId = Guid.Empty, // One-time payment
                UserId = userId,
                Type = SubscriptionTransactionType.Purchase,
                Amount = amount,
                Currency = currency,
                PaymentMethodId = paymentMethodId,
                ExternalTransactionId = externalTransactionId,
                Status = status,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                CompletedAt = success ? DateTime.UtcNow : null,
                FailedAt = !success ? DateTime.UtcNow : null,
                CreatedFromIP = createdFromIP
            };

            _context.SubscriptionTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                success ? "PAYMENT_PROCESSED" : "PAYMENT_FAILED",
                createdFromIP ?? "Unknown",
                null,
                success,
                $"{description}, Amount: {amount} {currency}, Transaction: {externalTransactionId}");

            return new PaymentResult
            {
                Success = success,
                Transaction = transaction,
                ExternalTransactionId = externalTransactionId,
                ErrorMessage = errorMessage,
                Status = status,
                RequiresAction = false
            };
        }
        catch (ArgumentException)
        {
            // Re-throw validation exceptions to be handled by the controller
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing one-time payment for user {UserId}", userId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Status = TransactionStatus.Failed
            };
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(
        Guid transactionId,
        Guid? requestingUserId = null,
        decimal? amount = null,
        string? reason = null,
        string? createdFromIP = null)
    {
        _logger.LogInformation("Processing refund for transaction {TransactionId}, amount {Amount}", transactionId, amount);

        try
        {
            var transaction = await _context.SubscriptionTransactions.FindAsync(transactionId);
            if (transaction == null)
                throw new ArgumentException("Transaction not found", nameof(transactionId));

            if (requestingUserId.HasValue && transaction.UserId != requestingUserId.Value)
            {
                _logger.LogWarning(
                    "User {RequestingUserId} attempted to refund transaction {TransactionId} owned by user {OwnerUserId}",
                    requestingUserId.Value, transactionId, transaction.UserId);
                throw new UnauthorizedAccessException("User is not authorized to refund this transaction");
            }

            var refundAmount = amount ?? transaction.Amount;
            var alreadyRefunded = transaction.RefundAmount ?? 0m;
            if (!string.IsNullOrWhiteSpace(transaction.ExternalChargeId))
            {
                var recordedRefundRows = await _context.SubscriptionTransactions
                    .Where(t => t.Type == SubscriptionTransactionType.Refund &&
                                t.Status == TransactionStatus.Completed &&
                                t.ExternalChargeId == transaction.ExternalChargeId)
                    .SumAsync(t => (decimal?)t.RefundAmount ?? t.Amount);

                alreadyRefunded = Math.Max(alreadyRefunded, recordedRefundRows);
            }

            var remainingRefundableAmount = transaction.Amount - alreadyRefunded;

            // BUG-FIN-010 FIX: Validate refund amount bounds
            if (refundAmount <= 0)
            {
                throw new ArgumentException("Refund amount must be greater than zero", nameof(amount));
            }

            if (refundAmount > transaction.Amount)
            {
                _logger.LogWarning(
                    "Refund amount {RefundAmount} exceeds original transaction amount {OriginalAmount} for transaction {TransactionId}",
                    refundAmount, transaction.Amount, transactionId);
                throw new ArgumentException(
                    $"Refund amount ({refundAmount:C}) cannot exceed the original transaction amount ({transaction.Amount:C})",
                    nameof(amount));
            }

            if (refundAmount > remainingRefundableAmount)
            {
                _logger.LogWarning(
                    "Refund amount {RefundAmount} exceeds remaining refundable amount {RemainingRefundableAmount} for transaction {TransactionId}",
                    refundAmount, remainingRefundableAmount, transactionId);
                throw new ArgumentException(
                    $"Refund amount ({refundAmount:C}) exceeds remaining refundable amount ({remainingRefundableAmount:C})",
                    nameof(amount));
            }

            // BUG-CRIT-002 FIX: Real Stripe refund processing
            string externalRefundId;
            string? externalChargeId = transaction.ExternalChargeId;
            bool success;
            string? errorMessage = null;
            TransactionStatus status;

            if (_stripeEnabled)
            {
                try
                {
                    // Process refund through Stripe
                    var refundService = new RefundService();
                    var options = new RefundCreateOptions
                    {
                        PaymentIntent = transaction.ExternalTransactionId,
                        Amount = (long)(refundAmount * 100), // Stripe uses cents
                        Reason = reason switch
                        {
                            "duplicate" => "duplicate",
                            "fraudulent" => "fraudulent",
                            _ => "requested_by_customer"
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "transaction_id", transactionId.ToString() },
                            { "user_id", transaction.UserId.ToString() }
                        }
                    };

                    var refund = await refundService.CreateAsync(options);
                    externalRefundId = refund.Id;
                    externalChargeId = refund.ChargeId ?? externalChargeId;
                    success = refund.Status == "succeeded";
                    status = success ? TransactionStatus.Completed : TransactionStatus.Failed;
                    errorMessage = success ? null : refund.FailureReason ?? "Refund failed";

                    _logger.LogInformation(
                        "Stripe refund {Status} for transaction {TransactionId}, Refund: {RefundId}",
                        refund.Status, transactionId, refund.Id);
                }
                catch (StripeException stripeEx)
                {
                    _logger.LogError(stripeEx, "Stripe error processing refund for transaction {TransactionId}", transactionId);
                    externalRefundId = $"stripe_error_{Guid.NewGuid():N}";
                    success = false;
                    status = TransactionStatus.Failed;
                    errorMessage = stripeEx.Message;
                }
            }
            else
            {
                // Stripe not enabled - fail with clear message
                _logger.LogWarning("Refund attempted but Stripe is not enabled (Transaction: {TransactionId})", transactionId);
                externalRefundId = $"no_provider_{Guid.NewGuid():N}";
                success = false;
                status = TransactionStatus.Failed;
                errorMessage = "Payment provider not configured. Please contact support.";
            }

            // Create refund transaction
            var refundTransaction = new SubscriptionTransaction
            {
                SubscriptionId = transaction.SubscriptionId,
                UserId = transaction.UserId,
                Type = SubscriptionTransactionType.Refund,
                Amount = refundAmount,
                Currency = transaction.Currency,
                PaymentMethodId = transaction.PaymentMethodId,
                ExternalTransactionId = externalRefundId,
                ExternalChargeId = externalChargeId,
                Status = status,
                Description = reason,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                CompletedAt = success ? DateTime.UtcNow : null,
                FailedAt = !success ? DateTime.UtcNow : null,
                RefundAmount = refundAmount,
                RefundedAt = success ? DateTime.UtcNow : null,
                CreatedFromIP = createdFromIP
            };

            _context.SubscriptionTransactions.Add(refundTransaction);
            if (success)
            {
                transaction.RefundAmount = alreadyRefunded + refundAmount;
                transaction.RefundedAt = DateTime.UtcNow;
                if (transaction.RefundAmount >= transaction.Amount)
                {
                    transaction.Status = TransactionStatus.Reversed;
                }
            }
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                transaction.UserId,
                success ? "REFUND_PROCESSED" : "REFUND_FAILED",
                createdFromIP ?? "Unknown",
                null,
                success,
                $"Refunded {refundAmount} {transaction.Currency}, Reason: {reason}, Transaction: {externalRefundId}");

            return new RefundResult
            {
                Success = success,
                RefundTransaction = refundTransaction,
                ExternalRefundId = externalRefundId,
                ErrorMessage = errorMessage,
                Status = status,
                RefundedAmount = refundAmount
            };
        }
        catch (ArgumentException)
        {
            // Re-throw validation exceptions to be handled by the controller
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return new RefundResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Status = TransactionStatus.Failed,
                RefundedAmount = amount ?? 0
            };
        }
    }

    public async Task<PaymentValidationResult> ValidatePaymentMethodAsync(
        Guid paymentMethodId,
        Guid userId)
    {
        var paymentMethod = await GetPaymentMethodAsync(paymentMethodId, userId);
        if (paymentMethod == null)
            return new PaymentValidationResult
            {
                IsValid = false,
                ErrorMessage = "Payment method not found"
            };

        // Check expiry date
        if (paymentMethod.ExpiresAt.HasValue && paymentMethod.ExpiresAt.Value < DateTime.UtcNow)
        {
            return new PaymentValidationResult
            {
                IsValid = false,
                IsExpired = true,
                ErrorMessage = "Payment method has expired",
                ExpiryDate = paymentMethod.ExpiresAt
            };
        }

        // MOCK IMPLEMENTATION - Replace with actual payment provider validation
        return new PaymentValidationResult
        {
            IsValid = paymentMethod.IsValid,
            IsExpired = false,
            ExpiryDate = paymentMethod.ExpiresAt
        };
    }

    public async Task<string> CreateExternalCustomerAsync(
        Guid userId,
        string email,
        string name)
    {
        _logger.LogInformation("Creating external customer for user {UserId}", userId);

        // MOCK IMPLEMENTATION - Replace with actual Stripe customer creation
        var externalCustomerId = $"cus_mock_{Guid.NewGuid():N}";

        // WARNING-002 FIX: ipAddress and userAgent cannot be null
        await _auditLogService.LogEventAsync(
            userId,
            "EXTERNAL_CUSTOMER_CREATED",
            string.Empty,
            string.Empty,
            true,
            $"Created external customer: {externalCustomerId}");

        return externalCustomerId;
    }

    public async Task<bool> UpdateExternalCustomerAsync(
        Guid userId,
        string email,
        string name)
    {
        _logger.LogInformation("Updating external customer for user {UserId}", userId);

        // MOCK IMPLEMENTATION - Replace with actual Stripe customer update
        // WARNING-002 FIX: ipAddress and userAgent cannot be null
        await _auditLogService.LogEventAsync(
            userId,
            "EXTERNAL_CUSTOMER_UPDATED",
            string.Empty,
            string.Empty,
            true,
            $"Updated external customer");

        return true;
    }

    public async Task<PaymentMethodDetails> GetPaymentMethodDetailsAsync(
        string paymentMethodToken,
        string provider)
    {
        _logger.LogInformation("Getting payment method details for token {Token}, provider {Provider}", paymentMethodToken, provider);

        // TEST MODE: Handle test tokens without calling Stripe API
        // Accept tokens starting with "tok_" (legacy) or "pm_test_" (new test format)
        if (provider.Equals("stripe", StringComparison.OrdinalIgnoreCase) &&
            (paymentMethodToken.StartsWith("tok_", StringComparison.OrdinalIgnoreCase) ||
             paymentMethodToken.StartsWith("pm_test_", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Using test mode for test token {Token}", paymentMethodToken);

            // Return mock payment method details for test tokens
            var cardBrand = paymentMethodToken.Contains("visa", StringComparison.OrdinalIgnoreCase) ? "visa" :
                           paymentMethodToken.Contains("mastercard", StringComparison.OrdinalIgnoreCase) ? "mastercard" : "visa";

            return await Task.FromResult(new PaymentMethodDetails
            {
                Last4Digits = "4242",
                Brand = cardBrand,
                ExpiryMonth = "12",
                ExpiryYear = "2029",
                CardholderName = "Test Cardholder",
                BillingCountry = "US",
                BillingPostalCode = "12345",
                IsValid = true,
                ExpiryDate = new DateTime(2029, 12, 31)
            });
        }

        // BUG-CRIT-002 FIX: Real Stripe API call to retrieve payment method details
        if (_stripeEnabled && provider.Equals("stripe", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var service = new Stripe.PaymentMethodService();
                var paymentMethod = await service.GetAsync(paymentMethodToken);

                if (paymentMethod.Card != null)
                {
                    var expiryMonth = paymentMethod.Card.ExpMonth.ToString();
                    var expiryYear = paymentMethod.Card.ExpYear.ToString();
                    DateTime? expiryDate = new DateTime((int)paymentMethod.Card.ExpYear, (int)paymentMethod.Card.ExpMonth, 1)
                        .AddMonths(1).AddDays(-1);

                    return new PaymentMethodDetails
                    {
                        Last4Digits = paymentMethod.Card.Last4,
                        Brand = paymentMethod.Card.Brand ?? "unknown",
                        ExpiryMonth = expiryMonth,
                        ExpiryYear = expiryYear,
                        CardholderName = paymentMethod.BillingDetails?.Name,
                        BillingCountry = paymentMethod.BillingDetails?.Address?.Country,
                        BillingPostalCode = paymentMethod.BillingDetails?.Address?.PostalCode,
                        IsValid = true,
                        ExpiryDate = expiryDate
                    };
                }

                _logger.LogWarning("Payment method {PaymentMethodId} has no card details", paymentMethodToken);
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe error retrieving payment method {PaymentMethodId}", paymentMethodToken);
                throw new InvalidOperationException($"Failed to retrieve payment method details: {stripeEx.Message}", stripeEx);
            }
        }

        // Fallback for when Stripe is not enabled (should not happen in production)
        _logger.LogWarning("Payment method details requested but Stripe is not enabled");
        throw new InvalidOperationException("Payment provider not configured");
    }

    public async Task<WebhookResult> ProcessWebhookAsync(
        string provider,
        string eventType,
        string eventData)
    {
        _logger.LogInformation("Processing webhook from {Provider}, event {EventType}", provider, eventType);

        var result = new WebhookResult();

        try
        {
            // MOCK IMPLEMENTATION - Replace with actual webhook processing logic
            // This would handle events like invoice.payment_succeeded, customer.subscription.deleted, etc.

            switch (eventType.ToLower())
            {
                case "invoice.payment_succeeded":
                    result.ProcessedEvents.Add(eventType);
                    break;
                case "invoice.payment_failed":
                    result.ProcessedEvents.Add(eventType);
                    break;
                case "customer.subscription.deleted":
                    result.ProcessedEvents.Add(eventType);
                    break;
                default:
                    result.FailedEvents.Add(eventType);
                    break;
            }

            result.Success = result.FailedEvents.Count == 0;

            _logger.LogInformation("Webhook processed successfully: {Success}", result.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from {Provider}, event {EventType}", provider, eventType);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.FailedEvents.Add(eventType);
            return result;
        }
    }
}
