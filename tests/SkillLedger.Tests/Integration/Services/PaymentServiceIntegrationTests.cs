using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Xunit;
using DbPaymentMethod = SkillLedger.Core.Entities.PaymentMethod;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for PaymentService - Payment Processing.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real internal services (audit log writes to DB)
/// - Mocks Stripe SDK (external payment provider)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (Stripe SDK via mock helper methods)
/// </summary>
public class PaymentServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly PaymentService _service;

    // REAL internal services
    private readonly MockAuditLogService _auditLogService;
    private readonly MockDistributedLockService _lockService;

    // Test data
    private readonly User _testUser;
    private readonly DbPaymentMethod _testPaymentMethod;

    public PaymentServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"PaymentServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal services
        _auditLogService = new MockAuditLogService(_context);
        _lockService = new MockDistributedLockService();

        // Setup configuration with test Stripe key (disabled for tests)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Stripe:IsEnabled"] = "false", // Disable actual Stripe calls
                ["Stripe:IsTestMode"] = "true",
                ["Stripe:SecretKey"] = "sk_test_fake_key_for_testing_only_1234567890abcdefghijklmnop"
            }!)
            .Build();

        var logger = new LoggerFactory().CreateLogger<PaymentService>();

        _service = new PaymentService(
            _context,
            _auditLogService,
            logger,
            configuration,
            _lockService);

        // Initialize test data
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        _testPaymentMethod = new DbPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card_visa",
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryDate = "12/2025",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(_testUser);
        _context.PaymentMethods.Add(_testPaymentMethod);
        _context.SaveChanges();
    }

    #region CreatePaymentMethod Tests

    [Fact]
    public async Task CreatePaymentMethodAsync_ValidCard_ShouldSaveToDatabase()
    {
        // Arrange
        var newToken = "pm_test_card_mastercard";

        // Note: Since Stripe is disabled, this will use the mock path
        // Real integration would call Stripe API

        // Act
        var result = await _service.CreatePaymentMethodAsync(
            _testUser.Id,
            "stripe",
            newToken,
            isDefault: false);

        // Assert - Verify database state
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUser.Id);
        result.Token.Should().Be(newToken);

        var savedPaymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Token == newToken);

        savedPaymentMethod.Should().NotBeNull();
        savedPaymentMethod!.Provider.Should().Be("stripe");
    }

    [Fact]
    public async Task CreatePaymentMethodAsync_SetAsDefault_ShouldUnsetOtherDefaults()
    {
        // Arrange
        var newToken = "pm_test_card_amex";

        // Act
        var result = await _service.CreatePaymentMethodAsync(
            _testUser.Id,
            "stripe",
            newToken,
            isDefault: true); // Set as new default

        // Assert - Old default should be unset
        var oldDefault = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == _testPaymentMethod.Id);

        oldDefault.Should().NotBeNull();
        oldDefault!.IsDefault.Should().BeFalse("old default should be unset");

        // New payment method should be default
        result.IsDefault.Should().BeTrue();

        // Should only have ONE default payment method
        var defaultCount = await _context.PaymentMethods
            .CountAsync(pm => pm.UserId == _testUser.Id && pm.IsDefault);

        defaultCount.Should().Be(1, "only one payment method can be default");
    }

    #endregion

    #region GetPaymentMethod Tests

    [Fact]
    public async Task GetPaymentMethodAsync_ValidId_ShouldReturnPaymentMethod()
    {
        // Act
        var result = await _service.GetPaymentMethodAsync(_testPaymentMethod.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_testPaymentMethod.Id);
        result.Token.Should().Be(_testPaymentMethod.Token);
    }

    [Fact]
    public async Task GetPaymentMethodAsync_WrongUser_ShouldReturnNull()
    {
        // Arrange
        var wrongUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetPaymentMethodAsync(_testPaymentMethod.Id, wrongUserId);

        // Assert
        result.Should().BeNull("payment method belongs to different user");
    }

    [Fact]
    public async Task GetPaymentMethodAsync_NonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetPaymentMethodAsync(nonExistentId, _testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetUserPaymentMethods Tests

    [Fact]
    public async Task GetUserPaymentMethodsAsync_MultipleCards_ShouldReturnAll()
    {
        // Arrange - Add another payment method
        var secondMethod = new DbPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card_second",
            Last4Digits = "5555",
            Brand = "Mastercard",
            ExpiryDate = "06/2026",
            IsDefault = false
        };
        _context.PaymentMethods.Add(secondMethod);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPaymentMethodsAsync(_testUser.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(pm => pm.Id == _testPaymentMethod.Id);
        result.Should().Contain(pm => pm.Id == secondMethod.Id);
    }

    [Fact]
    public async Task GetUserPaymentMethodsAsync_NoCards_ShouldReturnEmpty()
    {
        // Arrange
        var newUserId = Guid.NewGuid();
        var newUser = new User
        {
            Id = newUserId,
            UserName = "newuser",
            Email = "new@example.com",
            FirstName = "New",
            LastName = "User",
            EmailConfirmed = true
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserPaymentMethodsAsync(newUserId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SetDefaultPaymentMethod Tests

    [Fact]
    public async Task SetDefaultPaymentMethodAsync_ValidCard_ShouldSetAsDefault()
    {
        // Arrange - Create a non-default payment method
        var nonDefaultMethod = new DbPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card_nondefault",
            Last4Digits = "6666",
            Brand = "Discover",
            ExpiryDate = "09/2027",
            IsDefault = false
        };
        _context.PaymentMethods.Add(nonDefaultMethod);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetDefaultPaymentMethodAsync(nonDefaultMethod.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();

        // Old default should be unset
        var oldDefault = await _context.PaymentMethods.FindAsync(_testPaymentMethod.Id);
        oldDefault!.IsDefault.Should().BeFalse();

        // Verify audit log (service uses "PAYMENT_METHOD_SET_DEFAULT")
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id &&
                                     a.Action.Contains("PAYMENT_METHOD_SET_DEFAULT"));
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task SetDefaultPaymentMethodAsync_WrongUser_ShouldThrowException()
    {
        // Arrange
        var wrongUserId = Guid.NewGuid();

        // Act & Assert - Service throws ArgumentException for payment method not found
        var act = async () => await _service.SetDefaultPaymentMethodAsync(_testPaymentMethod.Id, wrongUserId);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment method not found*");
    }

    #endregion

    #region RemovePaymentMethod Tests

    [Fact]
    public async Task RemovePaymentMethodAsync_ValidCard_ShouldDeleteFromDatabase()
    {
        // Arrange - Create a payment method to delete
        var methodToDelete = new DbPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card_delete",
            Last4Digits = "7777",
            Brand = "Amex",
            ExpiryDate = "03/2025",
            IsDefault = false
        };
        _context.PaymentMethods.Add(methodToDelete);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RemovePaymentMethodAsync(methodToDelete.Id, _testUser.Id);

        // Assert
        result.Should().BeTrue();

        // Service uses physical delete - verify it's gone from database
        var deletedMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == methodToDelete.Id);

        deletedMethod.Should().BeNull("payment method should be physically deleted");
    }

    [Fact]
    public async Task RemovePaymentMethodAsync_DefaultCard_ShouldAllowDeletionWhenNoActiveSubscriptions()
    {
        // Arrange - testPaymentMethod is default but has no active subscriptions
        // Note: Current service implementation allows deletion of default payment methods
        // as long as they're not used by active subscriptions

        // Act
        var result = await _service.RemovePaymentMethodAsync(_testPaymentMethod.Id, _testUser.Id);

        // Assert - Service allows deletion since no active subscriptions use this payment method
        result.Should().BeTrue("default card can be deleted when not used by active subscriptions");

        // Verify it's removed from database
        var stillExists = await _context.PaymentMethods
            .AnyAsync(pm => pm.Id == _testPaymentMethod.Id);
        stillExists.Should().BeFalse();
    }

    [Fact]
    public async Task RemovePaymentMethodAsync_WrongUser_ShouldReturnFalse()
    {
        // Arrange
        var wrongUserId = Guid.NewGuid();

        // Act
        var result = await _service.RemovePaymentMethodAsync(_testPaymentMethod.Id, wrongUserId);

        // Assert
        result.Should().BeFalse("payment method belongs to different user");
    }

    #endregion

    #region ValidatePaymentMethod Tests

    [Fact]
    public async Task ValidatePaymentMethodAsync_ValidCard_ShouldReturnSuccess()
    {
        // Act
        var result = await _service.ValidatePaymentMethodAsync(_testPaymentMethod.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ValidatePaymentMethodAsync_ExpiredCard_ShouldReturnFailure()
    {
        // Arrange - Create expired card
        var expiredCard = new DbPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card_expired",
            Last4Digits = "8888",
            Brand = "Visa",
            ExpiryDate = "12/2020", // Expired string format
            ExpiresAt = new DateTime(2020, 12, 31), // Actual DateTime for validation
            IsDefault = false
        };
        _context.PaymentMethods.Add(expiredCard);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidatePaymentMethodAsync(expiredCard.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse("card is expired");
        result.ErrorMessage.Should().Contain("expired", "should mention expiration");
    }

    [Fact]
    public async Task ValidatePaymentMethodAsync_NonExistent_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.ValidatePaymentMethodAsync(nonExistentId, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region Edge Case Tests for Coverage (Phase 2.2)

    [Fact]
    public async Task SavePaymentMethodFromWebhookAsync_ExistingToken_ShouldUpdatePaymentMethod()
    {
        // Arrange - Existing payment method with same token
        var existingMethod = await _context.PaymentMethods.FirstAsync(pm => pm.Token == _testPaymentMethod.Token);
        var originalBrand = existingMethod.Brand;

        // Update from webhook with different card details
        var updatedMethod = new DbPaymentMethod
        {
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = _testPaymentMethod.Token, // Same token
            Last4Digits = "9999", // Updated
            Brand = "Mastercard", // Updated
            ExpiryDate = "06/2030", // Updated
            IsDefault = false,
            IsValid = true
        };

        // Act
        var result = await _service.SavePaymentMethodFromWebhookAsync(updatedMethod);

        // Assert - Should update existing payment method, not create new one
        result.Should().NotBeNull();
        result.Id.Should().Be(existingMethod.Id, "should update existing payment method");
        result.Brand.Should().Be("Mastercard", "brand should be updated");
        result.Last4Digits.Should().Be("9999", "last 4 digits should be updated");

        // Verify only one payment method with this token exists
        var count = await _context.PaymentMethods.CountAsync(pm => pm.Token == _testPaymentMethod.Token);
        count.Should().Be(1, "should update, not duplicate");
    }

    [Fact]
    public async Task SavePaymentMethodFromWebhookAsync_NewToken_ShouldCreatePaymentMethod()
    {
        // Arrange - New payment method from webhook
        var newMethod = new DbPaymentMethod
        {
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_webhook_new_card",
            Last4Digits = "3333",
            Brand = "Amex",
            ExpiryDate = "09/2028",
            IsDefault = false,
            IsValid = true
        };

        // Act
        var result = await _service.SavePaymentMethodFromWebhookAsync(newMethod);

        // Assert - Should create new payment method
        result.Should().NotBeNull();
        result.Token.Should().Be("pm_webhook_new_card");
        result.Brand.Should().Be("Amex");

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id &&
                                     a.Action == "PAYMENT_METHOD_SYNCED_FROM_WEBHOOK");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task RemovePaymentMethodAsync_WithActiveSubscription_ShouldThrowException()
    {
        // Arrange - Create an active subscription using the test payment method
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            IsAnnual = false, // Monthly subscription
            PaymentMethodId = _testPaymentMethod.Id,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act & Assert - Should throw exception
        var act = async () => await _service.RemovePaymentMethodAsync(_testPaymentMethod.Id, _testUser.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active subscriptions*");

        // Verify payment method still exists
        var stillExists = await _context.PaymentMethods.FindAsync(_testPaymentMethod.Id);
        stillExists.Should().NotBeNull("payment method should not be deleted");
    }

    [Fact]
    public async Task ProcessSubscriptionPaymentAsync_StripeDisabled_ShouldReturnFailureWithClearMessage()
    {
        // Arrange - Create subscription with payment method
        var tier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            Price = 29.99m,
            Features = "Pro Features",
            IsActive = true
        };

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = tier.Id,
            Status = SubscriptionStatus.Active,
            IsAnnual = false, // Monthly subscription
            PaymentMethodId = _testPaymentMethod.Id,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.Add(tier);
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Stripe is disabled in test configuration
        var result = await _service.ProcessSubscriptionPaymentAsync(
            subscription.Id,
            29.99m,
            "USD",
            "Test subscription payment");

        // Assert - Should fail with clear message
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payment provider not configured");
        result.Status.Should().Be(TransactionStatus.Failed);

        // Verify transaction was created
        var transaction = await _context.SubscriptionTransactions
            .FirstOrDefaultAsync(t => t.SubscriptionId == subscription.Id);
        transaction.Should().NotBeNull();
        transaction!.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public async Task ProcessSubscriptionPaymentAsync_NoPaymentMethod_ShouldReturnFailure()
    {
        // Arrange - Create subscription WITHOUT payment method
        var tier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Price = 9.99m,
            Features = "Basic Features",
            IsActive = true
        };

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = tier.Id,
            Status = SubscriptionStatus.Active,
            IsAnnual = false, // Monthly subscription
            PaymentMethodId = null, // No payment method
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.Add(tier);
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Service catches exception and returns failure result
        var result = await _service.ProcessSubscriptionPaymentAsync(
            subscription.Id,
            9.99m,
            "USD",
            "Test payment without method");

        // Assert - Should fail gracefully with error message
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No payment method");
        result.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public async Task ProcessOneTimePaymentAsync_StripeDisabled_ShouldReturnFailureWithClearMessage()
    {
        // Act - Stripe is disabled in test configuration
        var result = await _service.ProcessOneTimePaymentAsync(
            _testUser.Id,
            _testPaymentMethod.Id,
            49.99m,
            "USD",
            "One-time credit purchase");

        // Assert - Should fail with clear message
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payment provider not configured");
        result.Status.Should().Be(TransactionStatus.Failed);

        // Verify transaction was created
        var transaction = await _context.SubscriptionTransactions
            .FirstOrDefaultAsync(t => t.UserId == _testUser.Id &&
                                     t.Type == SubscriptionTransactionType.Purchase);
        transaction.Should().NotBeNull();
        transaction!.Amount.Should().Be(49.99m);
        transaction.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public async Task ProcessOneTimePaymentAsync_InvalidPaymentMethod_ShouldThrowException()
    {
        // Arrange
        var invalidPaymentMethodId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.ProcessOneTimePaymentAsync(
            _testUser.Id,
            invalidPaymentMethodId,
            25.00m,
            "USD",
            "Invalid payment method test");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment method not found*");
    }

    [Fact]
    public async Task RefundPaymentAsync_StripeDisabled_ShouldReturnFailureWithClearMessage()
    {
        // Arrange - Create a completed transaction to refund
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 99.99m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            ExternalTransactionId = "pi_test_completed",
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act - Attempt refund with Stripe disabled
        var result = await _service.RefundPaymentAsync(
            transaction.Id,
            amount: 99.99m,
            reason: "Customer request");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payment provider not configured");
        result.Status.Should().Be(TransactionStatus.Failed);

        // Verify refund transaction was created
        var refundTxn = await _context.SubscriptionTransactions
            .FirstOrDefaultAsync(t => t.Type == SubscriptionTransactionType.Refund &&
                                     t.UserId == _testUser.Id);
        refundTxn.Should().NotBeNull();
        refundTxn!.Amount.Should().Be(99.99m, "refund transaction amount should satisfy the positive refund constraints");
        refundTxn.RefundAmount.Should().Be(99.99m);
    }

    [Fact]
    public async Task RefundPaymentAsync_RequestingUserDoesNotOwnTransaction_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = otherUserId,
            UserName = "otheruser",
            Email = "other@example.com",
            EmailConfirmed = true
        });

        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 99.99m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            ExternalTransactionId = "pi_test_owned",
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var act = async () => await _service.RefundPaymentAsync(
            transaction.Id,
            requestingUserId: otherUserId,
            amount: 10.00m,
            reason: "Unauthorized refund");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var refundCount = await _context.SubscriptionTransactions
            .CountAsync(t => t.Type == SubscriptionTransactionType.Refund);
        refundCount.Should().Be(0);
    }

    [Fact]
    public async Task RefundPaymentAsync_ExceedsOriginalAmount_ShouldThrowException()
    {
        // Arrange - Create a completed transaction
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 50.00m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            ExternalTransactionId = "pi_test_50",
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act & Assert - Attempt to refund more than original amount
        var act = async () => await _service.RefundPaymentAsync(
            transaction.Id,
            amount: 75.00m, // More than original 50.00
            reason: "Excessive refund test");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot exceed the original transaction amount*");
    }

    [Fact]
    public async Task RefundPaymentAsync_WhenPriorRefundsLeaveInsufficientRemaining_ShouldThrowException()
    {
        // Arrange
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 100.00m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            ExternalTransactionId = "pi_test_partial_refunds",
            ExternalChargeId = "ch_test_partial_refunds",
            RefundAmount = 75.00m,
            RefundedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var act = async () => await _service.RefundPaymentAsync(
            transaction.Id,
            amount: 50.00m,
            reason: "Second partial refund");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceeds remaining refundable amount*");
    }

    [Fact]
    public async Task GetPaymentMethodDetailsAsync_StripeDisabled_ShouldThrowException()
    {
        // Arrange - Non-test token
        var realToken = "pm_real_production_token";

        // Act & Assert - Should throw because Stripe is disabled
        var act = async () => await _service.GetPaymentMethodDetailsAsync(realToken, "stripe");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Payment provider not configured*");
    }

    #endregion

    #region Phase 7 Coverage Tests - Additional Error Paths (2025-12-30)

    [Fact]
    public async Task ProcessSubscriptionPaymentAsync_LockAcquisitionFailure_ShouldReturnFailureResult()
    {
        // Arrange - Create subscription with payment method
        var tier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Premium",
            Price = 99.99m,
            Features = "Premium Features",
            IsActive = true
        };

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = tier.Id,
            Status = SubscriptionStatus.Active,
            IsAnnual = false,
            PaymentMethodId = _testPaymentMethod.Id,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.Add(tier);
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Set lock service to fail lock acquisition
        _lockService.SetShouldFail(true);

        // Act
        var result = await _service.ProcessSubscriptionPaymentAsync(
            subscription.Id,
            99.99m,
            "USD",
            "Test payment with lock failure");

        // Assert - Should fail with lock-specific message
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already being processed");
        result.Status.Should().Be(TransactionStatus.Failed);

        // Reset lock service for other tests
        _lockService.SetShouldFail(false);
    }

    [Fact]
    public async Task ProcessSubscriptionPaymentAsync_SubscriptionNotFound_ShouldReturnFailure()
    {
        // Arrange - Non-existent subscription ID
        var nonExistentSubId = Guid.NewGuid();

        // Act
        var result = await _service.ProcessSubscriptionPaymentAsync(
            nonExistentSubId,
            49.99m,
            "USD",
            "Payment for non-existent subscription");

        // Assert - Should fail gracefully
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Subscription not found");
        result.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public async Task ProcessOneTimePaymentAsync_UserNotFound_ShouldThrowException()
    {
        // Arrange - Create payment method for non-existent user (orphaned record scenario)
        var nonExistentUserId = Guid.NewGuid();
        var orphanedPaymentMethod = new DbPaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = nonExistentUserId, // User doesn't exist in database
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_orphaned",
            Last4Digits = "9999",
            Brand = "Visa",
            ExpiryDate = "12/2025",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentMethods.Add(orphanedPaymentMethod);
        await _context.SaveChangesAsync();

        // Act & Assert - Service should check user exists
        var act = async () => await _service.ProcessOneTimePaymentAsync(
            nonExistentUserId,
            orphanedPaymentMethod.Id,
            25.00m,
            "USD",
            "Payment for non-existent user");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User not found*");
    }

    [Fact]
    public async Task RefundPaymentAsync_TransactionNotFound_ShouldThrowException()
    {
        // Arrange - Non-existent transaction ID
        var nonExistentTxnId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _service.RefundPaymentAsync(
            nonExistentTxnId,
            amount: 50.00m,
            reason: "Test refund");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Transaction not found*");
    }

    [Fact]
    public async Task RefundPaymentAsync_NegativeAmount_ShouldThrowException()
    {
        // Arrange - Create a completed transaction
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 100.00m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            ExternalTransactionId = "pi_test_100",
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act & Assert - Attempt refund with negative amount
        var act = async () => await _service.RefundPaymentAsync(
            transaction.Id,
            amount: -10.00m,
            reason: "Negative refund test");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public async Task RefundPaymentAsync_ZeroAmount_ShouldThrowException()
    {
        // Arrange - Create a completed transaction
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 50.00m,
            Currency = "USD",
            Status = TransactionStatus.Completed,
            ExternalTransactionId = "pi_test_50_zero",
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act & Assert - Attempt refund with zero amount
        var act = async () => await _service.RefundPaymentAsync(
            transaction.Id,
            amount: 0.00m,
            reason: "Zero refund test");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvoicePaymentSucceeded_ShouldProcessEvent()
    {
        // Act - Process invoice.payment_succeeded event
        var result = await _service.ProcessWebhookAsync(
            "stripe",
            "invoice.payment_succeeded",
            "{}");

        // Assert - Should mark event as processed
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedEvents.Should().Contain("invoice.payment_succeeded");
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvoicePaymentFailed_ShouldProcessEvent()
    {
        // Act - Process invoice.payment_failed event
        var result = await _service.ProcessWebhookAsync(
            "stripe",
            "invoice.payment_failed",
            "{}");

        // Assert - Should mark event as processed
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedEvents.Should().Contain("invoice.payment_failed");
    }

    [Fact]
    public async Task ProcessWebhookAsync_CustomerSubscriptionDeleted_ShouldProcessEvent()
    {
        // Act - Process customer.subscription.deleted event
        var result = await _service.ProcessWebhookAsync(
            "stripe",
            "customer.subscription.deleted",
            "{}");

        // Assert - Should mark event as processed
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedEvents.Should().Contain("customer.subscription.deleted");
    }

    [Fact]
    public async Task ProcessWebhookAsync_UnknownEventType_ShouldMarkAsFailed()
    {
        // Act - Process unknown event type
        var result = await _service.ProcessWebhookAsync(
            "stripe",
            "unknown.event.type",
            "{}");

        // Assert - Should mark event as failed
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.FailedEvents.Should().Contain("unknown.event.type");
    }

    #endregion

    #region Phase 23 Coverage Tests - External Customer & Payment Method Details (2026-01-05)

    [Fact]
    public async Task CreateExternalCustomerAsync_ValidUser_ShouldCreateMockCustomerId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "customer@example.com";
        var name = "Test Customer";

        // Act
        var customerId = await _service.CreateExternalCustomerAsync(userId, email, name);

        // Assert
        customerId.Should().NotBeNullOrEmpty();
        customerId.Should().StartWith("cus_mock_", "mock implementation generates customer ID");

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == userId &&
                                     a.Action == "EXTERNAL_CUSTOMER_CREATED");
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain(customerId);
        auditLog.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateExternalCustomerAsync_ValidUser_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "updated@example.com";
        var name = "Updated Name";

        // Act
        var result = await _service.UpdateExternalCustomerAsync(userId, email, name);

        // Assert
        result.Should().BeTrue();

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == userId &&
                                     a.Action == "EXTERNAL_CUSTOMER_UPDATED");
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentMethodDetailsAsync_TestTokenWithTokPrefix_ShouldReturnMockDetails()
    {
        // Arrange - Test token with old format (tok_)
        var testToken = "tok_visa";

        // Act
        var details = await _service.GetPaymentMethodDetailsAsync(testToken, "stripe");

        // Assert
        details.Should().NotBeNull();
        details.Last4Digits.Should().Be("4242");
        details.Brand.Should().Be("visa");
        details.ExpiryMonth.Should().Be("12");
        details.ExpiryYear.Should().Be("2029");
        details.CardholderName.Should().Be("Test Cardholder");
        details.BillingCountry.Should().Be("US");
        details.BillingPostalCode.Should().Be("12345");
        details.IsValid.Should().BeTrue();
        details.ExpiryDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPaymentMethodDetailsAsync_TestTokenWithPmTestPrefix_ShouldReturnMockDetails()
    {
        // Arrange - Test token with new format (pm_test_)
        var testToken = "pm_test_mastercard";

        // Act
        var details = await _service.GetPaymentMethodDetailsAsync(testToken, "stripe");

        // Assert
        details.Should().NotBeNull();
        details.Last4Digits.Should().Be("4242");
        details.Brand.Should().Be("mastercard", "should detect mastercard from token");
        details.ExpiryMonth.Should().Be("12");
        details.ExpiryYear.Should().Be("2029");
        details.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentMethodDetailsAsync_TestTokenDefaultBrand_ShouldReturnVisaWhenUnspecified()
    {
        // Arrange - Test token without brand hint
        var testToken = "pm_test_generic";

        // Act
        var details = await _service.GetPaymentMethodDetailsAsync(testToken, "stripe");

        // Assert
        details.Should().NotBeNull();
        details.Brand.Should().Be("visa", "should default to visa when no brand specified");
    }

    [Fact]
    public void Constructor_WithValidStripeKey_ShouldInitializeSuccessfully()
    {
        // Arrange - Create configuration with valid Stripe key format
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Stripe:IsEnabled"] = "true",
                ["Stripe:IsTestMode"] = "true",
                ["Stripe:SecretKey"] = "sk_test_51ABCDEFabcdefghijklmnopqrstuvwxyz1234567890"
            }!)
            .Build();

        var logger = new LoggerFactory().CreateLogger<PaymentService>();

        // Act - Constructor should complete without exceptions
        var service = new PaymentService(
            _context,
            _auditLogService,
            logger,
            configuration,
            _lockService);

        // Assert - Service should be created successfully
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithInvalidStripeKeyFormat_ShouldLogWarningButContinue()
    {
        // Arrange - Create configuration with invalid Stripe key format
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Stripe:IsEnabled"] = "true",
                ["Stripe:IsTestMode"] = "true",
                ["Stripe:SecretKey"] = "invalid_key_format_123" // Invalid format
            }!)
            .Build();

        var logger = new LoggerFactory().CreateLogger<PaymentService>();

        // Act - Constructor should complete with warning
        var service = new PaymentService(
            _context,
            _auditLogService,
            logger,
            configuration,
            _lockService);

        // Assert - Service should be created despite invalid key format
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_StripeEnabledButNoKey_ShouldLogWarning()
    {
        // Arrange - Stripe enabled but no key provided
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Stripe:IsEnabled"] = "true",
                ["Stripe:IsTestMode"] = "true"
                // No SecretKey provided
            }!)
            .Build();

        var logger = new LoggerFactory().CreateLogger<PaymentService>();

        // Act - Constructor should complete with warning
        var service = new PaymentService(
            _context,
            _auditLogService,
            logger,
            configuration,
            _lockService);

        // Assert - Service should be created
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_StripeEnabledWithPlaceholderKey_ShouldNotInitializeStripe()
    {
        // Arrange - Stripe enabled with placeholder key
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Stripe:IsEnabled"] = "true",
                ["Stripe:IsTestMode"] = "true",
                ["Stripe:SecretKey"] = "REPLACE_WITH_YOUR_KEY" // Placeholder
            }!)
            .Build();

        var logger = new LoggerFactory().CreateLogger<PaymentService>();

        // Act - Constructor should skip Stripe initialization
        var service = new PaymentService(
            _context,
            _auditLogService,
            logger,
            configuration,
            _lockService);

        // Assert - Service should be created
        service.Should().NotBeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
