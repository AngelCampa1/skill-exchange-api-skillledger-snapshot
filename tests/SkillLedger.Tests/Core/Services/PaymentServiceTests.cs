using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// Tests for PaymentService - Critical financial service handling payment methods,
/// processing payments, and refunds.
/// Following TDD Red-Green-Refactor methodology
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Financial")]
public class PaymentServiceTests : IntegrationTestBase
{
    private readonly IPaymentService _paymentService;
    private User _testUser = null!;
    private User _testUser2 = null!;

    public PaymentServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _paymentService = ServiceScope.ServiceProvider.GetRequiredService<IPaymentService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "payment-user@example.com",
            UserName = "payment-user@example.com",
            NormalizedEmail = "PAYMENT-USER@EXAMPLE.COM",
            NormalizedUserName = "PAYMENT-USER@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ExternalCustomerId = $"cus_test_{Guid.NewGuid():N}"
        };
        Context.Users.Add(_testUser);

        _testUser2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "payment-user2@example.com",
            UserName = "payment-user2@example.com",
            NormalizedEmail = "PAYMENT-USER2@EXAMPLE.COM",
            NormalizedUserName = "PAYMENT-USER2@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ExternalCustomerId = $"cus_test_{Guid.NewGuid():N}"
        };
        Context.Users.Add(_testUser2);

        await Context.SaveChangesAsync();
    }

    #region Payment Method Creation Tests

    [Fact]
    public async Task CreatePaymentMethodAsync_TestToken_WorksWithoutStripeApiKey()
    {
        // Arrange
        var userId = _testUser.Id;
        var provider = "stripe";
        var token = $"pm_test_{Guid.NewGuid():N}";

        // Act - Test tokens (pm_test_*) should work without real Stripe API key by returning mock data
        var result = await _paymentService.CreatePaymentMethodAsync(
            userId, provider, token, isDefault: false, createdFromIP: "127.0.0.1");

        // Assert - Test token should create payment method successfully using mock data
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Provider.Should().Be(provider);
        result.Token.Should().Be(token);
        result.Last4Digits.Should().Be("4242"); // Mock data from GetPaymentMethodDetailsAsync
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task GetPaymentMethodAsync_ExistingMethod_ReturnsPaymentMethod()
    {
        // Arrange - Create a payment method directly in database
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_{Guid.NewGuid():N}",
            Last4Digits = "4242",
            Brand = "visa",
            ExpiryDate = "12/2030",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.GetPaymentMethodAsync(paymentMethod.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(paymentMethod.Id);
        result.Last4Digits.Should().Be("4242");
        result.Brand.Should().Be("visa");
    }

    [Fact]
    public async Task GetPaymentMethodAsync_WrongUser_ReturnsNull()
    {
        // Arrange - Create a payment method for user1
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_{Guid.NewGuid():N}",
            Last4Digits = "4242",
            Brand = "visa",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act - Try to get it with user2's ID
        var result = await _paymentService.GetPaymentMethodAsync(paymentMethod.Id, _testUser2.Id);

        // Assert - Should return null for security
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentMethodAsync_NonExistent_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _paymentService.GetPaymentMethodAsync(nonExistentId, _testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserPaymentMethodsAsync_MultipleExists_ReturnsOrdered()
    {
        // Arrange - Create multiple payment methods
        var paymentMethods = new[]
        {
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Provider = "stripe",
                Type = "card",
                Token = $"pm_test_1",
                Last4Digits = "1111",
                Brand = "visa",
                IsDefault = false,
                IsValid = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Provider = "stripe",
                Type = "card",
                Token = $"pm_test_2",
                Last4Digits = "2222",
                Brand = "mastercard",
                IsDefault = true, // Default should come first
                IsValid = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                Provider = "stripe",
                Type = "card",
                Token = $"pm_test_3",
                Last4Digits = "3333",
                Brand = "amex",
                IsDefault = false,
                IsValid = true,
                CreatedAt = DateTime.UtcNow, // Most recent
                UpdatedAt = DateTime.UtcNow
            }
        };
        Context.PaymentMethods.AddRange(paymentMethods);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.GetUserPaymentMethodsAsync(_testUser.Id);

        // Assert
        result.Should().HaveCount(3);
        result[0].IsDefault.Should().BeTrue(); // Default first
        result[0].Last4Digits.Should().Be("2222");
    }

    [Fact]
    public async Task GetUserPaymentMethodsAsync_NoMethods_ReturnsEmpty()
    {
        // Arrange - User has no payment methods

        // Act
        var result = await _paymentService.GetUserPaymentMethodsAsync(_testUser.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Set Default Payment Method Tests

    [Fact]
    public async Task SetDefaultPaymentMethodAsync_ValidMethod_SetsDefault()
    {
        // Arrange - Create two payment methods
        var paymentMethod1 = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = true, // Currently default
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var paymentMethod2 = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_2",
            Last4Digits = "2222",
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.AddRange(paymentMethod1, paymentMethod2);
        await Context.SaveChangesAsync();

        // Act - Set the second one as default
        var result = await _paymentService.SetDefaultPaymentMethodAsync(
            paymentMethod2.Id, _testUser.Id, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(paymentMethod2.Id);
        result.IsDefault.Should().BeTrue();

        // Verify the old default was unset
        var oldDefault = await Context.PaymentMethods.FindAsync(paymentMethod1.Id);
        oldDefault!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultPaymentMethodAsync_NonExistentMethod_ThrowsException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _paymentService.SetDefaultPaymentMethodAsync(
            nonExistentId, _testUser.Id, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment method not found*");
    }

    [Fact]
    public async Task SetDefaultPaymentMethodAsync_WrongUser_ThrowsException()
    {
        // Arrange - Create payment method for user1
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act - Try to set default with user2's ID
        Func<Task> act = async () => await _paymentService.SetDefaultPaymentMethodAsync(
            paymentMethod.Id, _testUser2.Id, "127.0.0.1");

        // Assert - Should throw since user2 doesn't own this method
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment method not found*");
    }

    #endregion

    #region Remove Payment Method Tests

    [Fact]
    public async Task RemovePaymentMethodAsync_ValidMethod_RemovesAndReturnsTrue()
    {
        // Arrange
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.RemovePaymentMethodAsync(
            paymentMethod.Id, _testUser.Id, "127.0.0.1");

        // Assert
        result.Should().BeTrue();

        var removed = await Context.PaymentMethods.FindAsync(paymentMethod.Id);
        removed.Should().BeNull();
    }

    [Fact]
    public async Task RemovePaymentMethodAsync_NonExistentMethod_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _paymentService.RemovePaymentMethodAsync(
            nonExistentId, _testUser.Id, "127.0.0.1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemovePaymentMethodAsync_UsedByActiveSubscription_ThrowsException()
    {
        // Arrange - Create payment method and subscription
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);

        // Create a subscription tier first
        var tier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Test Tier",
            Price = 9.99m,
            AnnualPrice = 99.99m,
            CreditBonus = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.SubscriptionTiers.Add(tier);

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = tier.Id,
            PaymentMethodId = paymentMethod.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _paymentService.RemovePaymentMethodAsync(
            paymentMethod.Id, _testUser.Id, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot remove payment method used by active subscriptions*");
    }

    #endregion

    #region Validate Payment Method Tests

    [Fact]
    public async Task ValidatePaymentMethodAsync_ValidMethod_ReturnsValid()
    {
        // Arrange
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = true,
            IsValid = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1), // Valid for another year
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.ValidatePaymentMethodAsync(paymentMethod.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePaymentMethodAsync_ExpiredMethod_ReturnsExpired()
    {
        // Arrange
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = true,
            IsValid = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.ValidatePaymentMethodAsync(paymentMethod.Id, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task ValidatePaymentMethodAsync_NonExistentMethod_ReturnsInvalid()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _paymentService.ValidatePaymentMethodAsync(nonExistentId, _testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    #endregion

    #region Process One-Time Payment Tests

    [Fact]
    public async Task ProcessOneTimePaymentAsync_WithoutStripeApiKey_FailsGracefully()
    {
        // Arrange
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.ProcessOneTimePaymentAsync(
            _testUser.Id,
            paymentMethod.Id,
            amount: 10.00m,
            currency: "USD",
            description: "Test payment",
            createdFromIP: "127.0.0.1");

        // Assert - Without Stripe API key configured, payment fails gracefully
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        // Error message can be from Stripe SDK (no API key) or our validation
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public async Task ProcessOneTimePaymentAsync_InvalidPaymentMethod_ThrowsException()
    {
        // Arrange
        var nonExistentPaymentMethodId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _paymentService.ProcessOneTimePaymentAsync(
            _testUser.Id,
            nonExistentPaymentMethodId,
            amount: 10.00m,
            currency: "USD",
            description: "Test payment",
            createdFromIP: "127.0.0.1");

        // Assert - Service throws for invalid payment method
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment method not found*");
    }

    [Fact]
    public async Task ProcessOneTimePaymentAsync_WrongUserPaymentMethod_ThrowsException()
    {
        // Arrange
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_test_1",
            Last4Digits = "1111",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.PaymentMethods.Add(paymentMethod);
        await Context.SaveChangesAsync();

        var nonExistentUserId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _paymentService.ProcessOneTimePaymentAsync(
            nonExistentUserId,
            paymentMethod.Id,
            amount: 10.00m,
            currency: "USD",
            description: "Test payment",
            createdFromIP: "127.0.0.1");

        // Assert - Service throws (payment method belongs to different user)
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Payment method not found*");
    }

    #endregion

    #region Refund Payment Tests

    [Fact]
    public async Task RefundPaymentAsync_FullRefund_FailsWithoutStripeApiKey()
    {
        // Arrange - Create a transaction to refund
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 100.00m,
            Currency = "USD",
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.RefundPaymentAsync(
            transaction.Id,
            amount: null, // Full refund
            reason: "Customer request",
            createdFromIP: "127.0.0.1");

        // Assert - Without Stripe API key configured, refund fails gracefully
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        // Error message can be either from Stripe SDK (no API key) or our validation
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefundPaymentAsync_PartialRefund_RefundsCorrectAmount()
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
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        // Act
        var result = await _paymentService.RefundPaymentAsync(
            transaction.Id,
            amount: 50.00m, // Partial refund
            reason: "Partial refund",
            createdFromIP: "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.RefundedAmount.Should().Be(50.00m);
    }

    [Fact]
    public async Task RefundPaymentAsync_AmountExceedsTransaction_ThrowsException()
    {
        // Arrange
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 50.00m,
            Currency = "USD",
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _paymentService.RefundPaymentAsync(
            transaction.Id,
            amount: 100.00m, // More than original transaction
            reason: "Over-refund attempt",
            createdFromIP: "127.0.0.1");

        // Assert - Service throws for invalid amount
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot exceed*");
    }

    [Fact]
    public async Task RefundPaymentAsync_ZeroAmount_ThrowsException()
    {
        // Arrange
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 50.00m,
            Currency = "USD",
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _paymentService.RefundPaymentAsync(
            transaction.Id,
            amount: 0m,
            reason: "Zero refund",
            createdFromIP: "127.0.0.1");

        // Assert - Service throws for zero amount
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task RefundPaymentAsync_NegativeAmount_ThrowsException()
    {
        // Arrange
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _testUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 50.00m,
            Currency = "USD",
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _paymentService.RefundPaymentAsync(
            transaction.Id,
            amount: -10.00m,
            reason: "Negative refund",
            createdFromIP: "127.0.0.1");

        // Assert - Service throws for negative amount
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task RefundPaymentAsync_NonExistentTransaction_ThrowsException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _paymentService.RefundPaymentAsync(
            nonExistentId,
            amount: null,
            reason: "Refund non-existent",
            createdFromIP: "127.0.0.1");

        // Assert - Service throws for non-existent transaction
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Transaction not found*");
    }

    #endregion

    #region External Customer Tests

    [Fact]
    public async Task CreateExternalCustomerAsync_ValidUser_ReturnsCustomerId()
    {
        // Arrange
        var userId = _testUser.Id;
        var email = _testUser.Email!;
        var name = "Test User";

        // Act
        var result = await _paymentService.CreateExternalCustomerAsync(userId, email, name);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("cus_"); // Mock format
    }

    [Fact]
    public async Task UpdateExternalCustomerAsync_ValidUser_ReturnsTrue()
    {
        // Arrange
        var userId = _testUser.Id;
        var email = "updated-email@example.com";
        var name = "Updated Name";

        // Act
        var result = await _paymentService.UpdateExternalCustomerAsync(userId, email, name);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Webhook Processing Tests

    [Fact]
    public async Task ProcessWebhookAsync_ValidInvoicePaymentSucceeded_ProcessesEvent()
    {
        // Arrange
        var provider = "stripe";
        var eventType = "invoice.payment_succeeded";
        var eventData = "{}";

        // Act
        var result = await _paymentService.ProcessWebhookAsync(provider, eventType, eventData);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedEvents.Should().Contain(eventType);
    }

    [Fact]
    public async Task ProcessWebhookAsync_ValidInvoicePaymentFailed_ProcessesEvent()
    {
        // Arrange
        var provider = "stripe";
        var eventType = "invoice.payment_failed";
        var eventData = "{}";

        // Act
        var result = await _paymentService.ProcessWebhookAsync(provider, eventType, eventData);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedEvents.Should().Contain(eventType);
    }

    [Fact]
    public async Task ProcessWebhookAsync_ValidSubscriptionDeleted_ProcessesEvent()
    {
        // Arrange
        var provider = "stripe";
        var eventType = "customer.subscription.deleted";
        var eventData = "{}";

        // Act
        var result = await _paymentService.ProcessWebhookAsync(provider, eventType, eventData);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedEvents.Should().Contain(eventType);
    }

    [Fact]
    public async Task ProcessWebhookAsync_UnknownEventType_FailsEvent()
    {
        // Arrange
        var provider = "stripe";
        var eventType = "unknown.event.type";
        var eventData = "{}";

        // Act
        var result = await _paymentService.ProcessWebhookAsync(provider, eventType, eventData);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.FailedEvents.Should().Contain(eventType);
    }

    #endregion

    #region Save Payment Method From Webhook Tests

    [Fact]
    public async Task SavePaymentMethodFromWebhookAsync_NewMethod_CreatesMethod()
    {
        // Arrange
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = $"pm_webhook_{Guid.NewGuid():N}",
            Last4Digits = "5678",
            Brand = "mastercard",
            ExpiryDate = "06/2028",
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _paymentService.SavePaymentMethodFromWebhookAsync(paymentMethod);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be(paymentMethod.Token);

        var saved = await Context.PaymentMethods.FirstOrDefaultAsync(pm => pm.Token == paymentMethod.Token);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task SavePaymentMethodFromWebhookAsync_ExistingToken_UpdatesMethod()
    {
        // Arrange - Create existing payment method
        var existingToken = $"pm_webhook_{Guid.NewGuid():N}";
        var existing = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = existingToken,
            Last4Digits = "1234",
            Brand = "visa",
            ExpiryDate = "12/2025",
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        Context.PaymentMethods.Add(existing);
        await Context.SaveChangesAsync();

        // Create update with same token but different details
        var update = new PaymentMethod
        {
            Id = Guid.NewGuid(), // Different ID
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = existingToken, // Same token
            Last4Digits = "5678", // Updated
            Brand = "mastercard", // Updated
            ExpiryDate = "06/2030", // Updated
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _paymentService.SavePaymentMethodFromWebhookAsync(update);

        // Assert - Should return the existing (updated) payment method
        result.Should().NotBeNull();
        result.Id.Should().Be(existing.Id); // Same ID
        result.Last4Digits.Should().Be("5678"); // Updated value
        result.Brand.Should().Be("mastercard"); // Updated value
    }

    #endregion
}
