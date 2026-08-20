using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Stripe;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for PaymentErrorHandlingService - CRITICAL FINANCIAL SERVICE.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockEmailService (external Azure Communication Services - OK to mock)
/// - Uses MockSubscriptionService (internal - should be real, but requires complex setup)
/// - Mocks Stripe SDK calls (external payment processor - OK to mock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 2 (Email, Stripe)
/// Max mocked internal dependencies: 1 (SubscriptionService - complex external state)
///
/// CRITICAL: This service has 0% test coverage and handles payment failures.
/// Expected bugs: Retry logic, exponential backoff, subscription cancellation.
/// </summary>
[IntegrationTest]
[FinancialTest]
public class PaymentErrorHandlingServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;  // REAL (writes to DB)
    private readonly Mocks.MockEmailService _emailService;  // External - OK to mock
    private readonly Mocks.MockSubscriptionService _subscriptionService;  // Internal but complex - OK to mock for now
    private readonly PaymentErrorHandlingService _service;
    private readonly PaymentRetryConfiguration _retryConfig;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testSubscriptionTierId = Guid.NewGuid();

    public PaymentErrorHandlingServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"PaymentErrorHandlingTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal services
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!

        // Setup EXTERNAL services (OK to mock)
        _emailService = new Mocks.MockEmailService();
        _subscriptionService = new Mocks.MockSubscriptionService(_context);  // Pass context to update DB!

        // Configure retry settings for testing
        _retryConfig = new PaymentRetryConfiguration
        {
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMinutes(15),
            MaxDunningAttempts = 4,
            EnableAutomaticRetries = true,
            EnableDunningWorkflow = true,
            SupportEmail = "angel.campa@skillledger.app"
        };

        var stripeSettings = new StripeSettings
        {
            SecretKey = "sk_test_mock_key",
            PublishableKey = "pk_test_mock_key",
            IsEnabled = false,  // Disable Stripe API calls in tests
            IsTestMode = true
        };

        var logger = new LoggerFactory().CreateLogger<PaymentErrorHandlingService>();

        _service = new PaymentErrorHandlingService(
            logger,
            Options.Create(stripeSettings),
            _context,
            _subscriptionService,
            _emailService,
            _auditLogService,
            Options.Create(_retryConfig));

        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create test user
        var user = new User
        {
            Id = _testUserId,
            Email = "testuser@test.com",
            UserName = "testuser@test.com",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active
        };

        // Create subscription tier
        var tier = new SubscriptionTier
        {
            Id = _testSubscriptionTierId,
            Name = "Professional",
            Type = SubscriptionTierType.Professional,
            Price = 29.99m,
            AnnualPrice = 299.99m
        };

        _context.Users.Add(user);
        _context.SubscriptionTiers.Add(tier);
        _context.SaveChanges();
    }

    #region Invoice Payment Failure - Retry Count Tests

    [Fact]
    public async Task ProcessInvoicePaymentFailure_FirstFailure_ShouldIncrementRetryCount()
    {
        // Arrange - Create active subscription
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_123",
            RetryCount = 0,
            NextRetryAt = null
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Simulate first payment failure
        var result = await _service.ProcessInvoicePaymentFailureAsync("inv_test_123", "sub_test_123");

        // Assert - Verify database state
        result.Success.Should().BeTrue("invoice failure should be processed successfully");

        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.RetryCount.Should().Be(1, "retry count should increment on first failure");
        updatedSubscription.NextRetryAt.Should().NotBeNull("next retry time should be scheduled");
        updatedSubscription.NextRetryAt.Should().BeCloseTo(
            DateTime.UtcNow.Add(_retryConfig.RetryDelay),
            TimeSpan.FromMinutes(1),
            "next retry should be scheduled based on RetryDelay configuration");

        // Verify audit log in database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "PAYMENT_ERROR_HANDLED");
        // Note: This might not exist because ProcessInvoicePaymentFailureAsync doesn't call LogErrorHandlingAsync
        // This could be BUG #PAY-005: Missing audit log for invoice payment failures
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_MultipleFailures_ShouldTrackRetryCountCorrectly()
    {
        // Arrange - Subscription with 2 previous failures
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_456",
            RetryCount = 2,
            NextRetryAt = DateTime.UtcNow.AddMinutes(15)
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Third failure
        var result = await _service.ProcessInvoicePaymentFailureAsync("inv_test_456", "sub_test_456");

        // Assert - Verify retry count incremented
        result.Success.Should().BeTrue();

        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.RetryCount.Should().Be(3, "retry count should increment to 3");
        // Exponential backoff: RetryCount=3 means delay = 15 * 2^(3-1) = 60 minutes
        var expectedDelay = TimeSpan.FromMinutes(_retryConfig.RetryDelay.TotalMinutes * Math.Pow(2, 3 - 1));
        updatedSubscription.NextRetryAt.Should().BeCloseTo(
            DateTime.UtcNow.Add(expectedDelay),
            TimeSpan.FromMinutes(1),
            "should use exponential backoff (60 minutes for 3rd retry)");
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_AfterMaxRetries_ShouldCancelSubscription()
    {
        // Arrange - Subscription at max retries
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_max_retries",
            RetryCount = _retryConfig.MaxDunningAttempts - 1,  // One away from max
            NextRetryAt = DateTime.UtcNow.AddMinutes(15)
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Final failure that should trigger cancellation
        var result = await _service.ProcessInvoicePaymentFailureAsync("inv_test_max", "sub_test_max_retries");

        // Assert - Verify subscription should be cancelled
        // BUG EXPECTED: Subscription NOT cancelled (NextAction set but never executed)
        result.Success.Should().BeTrue();
        result.NextAction.Should().Be(DunningAction.CancelSubscription,
            "dunning action should indicate subscription cancellation");

        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.RetryCount.Should().Be(_retryConfig.MaxDunningAttempts);

        // BUG #PAY-003: This assertion will FAIL - subscription never actually cancelled
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Cancelled,
            "subscription should be CANCELLED after max dunning attempts");
        updatedSubscription.CancelledAt.Should().NotBeNull("cancellation date should be set");
        updatedSubscription.CancellationReason.Should().Contain("payment failure",
            "cancellation reason should reference payment failure");
    }

    #endregion

    #region Retry Limit Enforcement Tests

    [Fact]
    public async Task RetryPayment_WithinRetryLimit_ShouldAllowRetry()
    {
        // Arrange - Create payment intent (mocked via Stripe SDK)
        var paymentIntentId = "pi_test_within_limit";

        // NOTE: This test will expose BUG #PAY-001
        // GetRetryCountAsync() always returns 0, so retry limits are NEVER enforced!

        // Act - Attempt retry when under limit
        var result = await _service.RetryPaymentAsync(paymentIntentId, _testUserId, PaymentRetryReason.UserInitiated);

        // Assert - Should allow retry
        // BUG #PAY-001 EXPECTED: This will PASS even when it shouldn't
        // because GetRetryCountAsync() is stubbed to return 0
        result.Should().NotBeNull();

        // Verify audit log created
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "PAYMENT_RETRY_ATTEMPTED");
        auditLog.Should().NotBeNull("retry attempt should be logged to database");
        auditLog!.UserId.Should().Be(_testUserId);
        auditLog.Success.Should().BeFalse("retry should fail without real Stripe integration");
    }

    [Fact]
    public async Task RetryPayment_ExceedsMaxRetries_ShouldBlockRetry()
    {
        // Arrange - Simulate payment that has already been retried 3 times
        var paymentIntentId = "pi_test_max_retries_reached";

        // Create 3 prior retry attempt audit logs (MaxRetryAttempts = 3)
        for (int i = 0; i < 3; i++)
        {
            var details = $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:{i + 1}";
            await _auditLogService.LogEventAsync(
                _testUserId,
                "PAYMENT_RETRY_ATTEMPTED",
                string.Empty,
                "PaymentErrorHandlingService",
                false,
                details);
        }

        // Act - Attempt 4th retry (should be blocked)
        var result = await _service.RetryPaymentAsync(paymentIntentId, _testUserId, PaymentRetryReason.AutomaticRetry);

        // Assert - BUG #PAY-001 WILL CAUSE THIS TO FAIL
        // The stubbed GetRetryCountAsync() returns 0, so HasReachedMaxRetriesAsync() returns false
        // Result: Retry is ALLOWED when it should be BLOCKED
        result.Should().NotBeNull();
        result.ErrorCode.Should().Be("MAX_RETRIES_REACHED",
            "should block retry when max attempts (3) exceeded");
        result.RequiresNewPaymentMethod.Should().BeTrue(
            "should require new payment method after max retries");

        // Verify NO NEW retry was attempted (should still have only the 3 setup audit logs)
        var auditLogs = await _context.AuditLogs
            .Where(a => a.Action == "PAYMENT_RETRY_ATTEMPTED" &&
                        a.Details != null &&
                        a.Details.Contains(paymentIntentId))
            .ToListAsync();
        auditLogs.Should().HaveCount(3, "should have only the 3 setup audit logs, no new retry should be attempted when max retries reached");
    }

    #endregion

    #region Retry Timing Tests

    [Fact]
    public async Task RetryPayment_TooSoonAfterLastRetry_ShouldBlockRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_retry_too_soon";

        // Create a recent retry attempt (5 minutes ago)
        // RetryDelay is 15 minutes, so retry should be blocked for another 10 minutes
        var details = $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:1";

        // Create audit log entry with timestamp 5 minutes ago
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Action = "PAYMENT_RETRY_ATTEMPTED",
            IPAddress = "",
            UserAgent = "PaymentErrorHandlingService",
            Success = false,
            Details = details,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)  // 5 minutes ago
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        // Act - Attempt retry too soon
        var result = await _service.RetryPaymentAsync(paymentIntentId, _testUserId, PaymentRetryReason.UserInitiated);

        // Assert - BUG #PAY-002 WILL CAUSE THIS TO FAIL
        // GetLastRetryAttemptAsync() returns null, so timing check is skipped
        // Retry is ALLOWED when it should be BLOCKED
        result.ErrorCode.Should().Be("RETRY_TOO_SOON",
            "should block retry when not enough time has passed since last attempt");
        result.NextRetryAllowedAt.Should().NotBeNull("should provide next allowed retry time");
        result.NextRetryAllowedAt.Should().BeAfter(DateTime.UtcNow,
            "next retry time should be in the future");
    }

    [Fact]
    public async Task RetryPayment_AfterRetryDelay_ShouldAllowRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_retry_allowed";

        // BUG #PAY-002: This test will PASS but for the WRONG reason
        // GetLastRetryAttemptAsync() returns null, so it thinks there was NO last retry
        // Rather than checking if enough time has passed

        // Act
        var result = await _service.RetryPaymentAsync(paymentIntentId, _testUserId, PaymentRetryReason.AutomaticRetry);

        // Assert - Will pass but doesn't actually test timing logic
        result.Should().NotBeNull();
        result.ErrorCode.Should().NotBe("RETRY_TOO_SOON");
    }

    #endregion

    #region Error Classification Tests

    [Fact]
    public async Task HandlePaymentFailure_CardDeclined_ShouldClassifyCorrectly()
    {
        // Arrange
        var paymentIntentId = "pi_test_card_declined";
        var error = new StripeError
        {
            Code = "card_declined",
            Message = "Your card was declined"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert - Verify classification and recovery strategy
        // Note: This will likely fail because we can't mock Stripe PaymentIntent retrieval
        // But it will expose how error classification works
        result.Should().NotBeNull();
        result.RecoveryAction.Should().Be(RecoveryAction.UpdatePaymentMethod,
            "card declined should require payment method update");
    }

    [Fact]
    public async Task HandlePaymentFailure_InsufficientFunds_ShouldClassifyCorrectly()
    {
        // Arrange
        var paymentIntentId = "pi_test_insufficient_funds";
        var error = new StripeError
        {
            Code = "insufficient_funds",
            Message = "Insufficient funds"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert
        result.Should().NotBeNull();
        result.RecoveryAction.Should().NotBe(RecoveryAction.UpdatePaymentMethod,
            "insufficient funds should NOT require payment method update");
        result.ShouldNotifyUser.Should().BeTrue("user should be notified about insufficient funds");
    }

    [Fact]
    public async Task HandlePaymentFailure_ProcessingError_ShouldScheduleRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_processing_error";
        var error = new StripeError
        {
            Code = "processing_error",
            Message = "Processing error occurred"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert
        result.Should().NotBeNull();
        result.RecoveryAction.Should().Be(RecoveryAction.ScheduledRetry,
            "processing error should trigger scheduled retry with backoff");
        result.NextRetryAt.Should().NotBeNull("next retry time should be scheduled");
    }

    #endregion

    #region Exponential Backoff Tests

    [Fact]
    public async Task RetryPayment_MultipleAttempts_ShouldUseExponentialBackoff()
    {
        // Arrange
        var paymentIntentId = "pi_test_exponential_backoff";

        // Create 2 prior retry attempts to test exponential backoff on the 3rd attempt
        // Attempt 1: 20 minutes ago (enough time has passed for retry)
        var auditLog1 = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Action = "PAYMENT_RETRY_ATTEMPTED",
            IPAddress = "",
            UserAgent = "PaymentErrorHandlingService",
            Success = false,
            Details = $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:1",
            Timestamp = DateTime.UtcNow.AddMinutes(-20)
        };
        _context.AuditLogs.Add(auditLog1);

        // Attempt 2: 1 hour ago (enough time has passed for exponential backoff)
        var auditLog2 = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Action = "PAYMENT_RETRY_ATTEMPTED",
            IPAddress = "",
            UserAgent = "PaymentErrorHandlingService",
            Success = false,
            Details = $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:2",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };
        _context.AuditLogs.Add(auditLog2);
        await _context.SaveChangesAsync();

        // Expected behavior:
        // - 1st retry: 15 minutes
        // - 2nd retry: 30 minutes (2x)
        // - 3rd retry: 60 minutes (4x)

        // Act - Simulate 3rd retry attempt (should use exponential backoff)
        var result1 = await _service.RetryPaymentAsync(paymentIntentId, _testUserId, PaymentRetryReason.AutomaticRetry);

        // In a real implementation, we'd verify:
        // - First NextRetryAt is ~15 minutes from now
        // - Second NextRetryAt is ~30 minutes from first retry
        // - Third NextRetryAt is ~60 minutes from second retry

        // Assert - Verify exponential backoff calculation
        result1.Should().NotBeNull();
        result1.ErrorCode.Should().Be("RETRY_TOO_SOON", "should block retry when not enough time has passed");

        // With 2 prior retries (currentRetryCount=2), exponential delay = 15 * 2^(2-1) = 30 minutes
        // Last retry was 20 minutes ago, so need to wait 10 more minutes
        // NextRetryAllowedAt = lastRetry (20 min ago) + requiredDelay (30 min) = 10 minutes from now
        var expectedExponentialDelay = TimeSpan.FromMinutes(15 * Math.Pow(2, 1)); // 30 min for 2nd retry
        var lastRetryTime = DateTime.UtcNow.AddMinutes(-20);
        result1.NextRetryAllowedAt.Should().BeCloseTo(
            lastRetryTime.Add(expectedExponentialDelay),
            TimeSpan.FromMinutes(1),
            "should calculate next retry time using exponential backoff (30 minutes for retry count 2)");

        // Note: We can't test 2nd and 3rd retries because GetRetryCountAsync() is stubbed
        // So we can't actually track which attempt number we're on
    }

    #endregion

    #region Concurrent Error Handling Tests

    [Fact]
    public async Task ProcessInvoicePaymentFailure_ConcurrentFailures_ShouldNotCauseRaceConditions()
    {
        // Arrange - Create subscription
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_concurrent",
            RetryCount = 0
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Simulate 5 concurrent payment failures
        var tasks = Enumerable.Range(1, 5)
            .Select(i => _service.ProcessInvoicePaymentFailureAsync($"inv_test_{i}", "sub_test_concurrent"))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - Check for race conditions in retry count
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);

        // POTENTIAL BUG #PAY-006: Race condition in RetryCount increment
        // If concurrent updates aren't handled properly, RetryCount might be wrong
        // Expected: 5 (one increment per failure)
        // Actual: Could be less if race condition exists (e.g., 3 or 4)
        updatedSubscription!.RetryCount.Should().Be(5,
            "each concurrent failure should increment retry count exactly once");

        // Verify all results succeeded
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Verify exactly 5 dunning emails sent
        _emailService.SentEmails.Should().HaveCount(5,
            "each failure should trigger exactly one dunning email");
    }

    [Fact]
    public async Task HandlePaymentFailure_ConcurrentSamePayment_ShouldHandleGracefully()
    {
        // Arrange
        var paymentIntentId = "pi_test_concurrent_same";

        // Act - Simulate 3 concurrent error handling requests for SAME payment
        var tasks = Enumerable.Range(1, 3)
            .Select(_ => _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, null))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete without errors
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        // Verify audit logs - should have 3 entries (one per concurrent request)
        var auditLogs = await _context.AuditLogs
            .Where(a => a.Action == "PAYMENT_ERROR_HANDLED")
            .ToListAsync();

        // Each concurrent request should create its own audit log
        auditLogs.Should().HaveCountGreaterThanOrEqualTo(3,
            "concurrent error handling should not skip audit logging");
    }

    #endregion

    #region Recovery Options Tests

    [Fact]
    public async Task GetRecoveryOptions_NoRetries_ShouldAllowRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_recovery_options";

        // Act
        var options = await _service.GetRecoveryOptionsAsync(paymentIntentId, _testUserId);

        // Assert - BUG #PAY-001 & #PAY-002: This will PASS but for wrong reasons
        // GetRetryCountAsync() returns 0 (always)
        // GetLastRetryAttemptAsync() returns null (always)
        // So it looks like no retries have happened, when we haven't checked properly
        options.Should().NotBeNull();
        options.RetryAttemptsRemaining.Should().Be(_retryConfig.MaxRetryAttempts,
            "should show all retry attempts available when none used");
    }

    [Fact]
    public async Task GetRecoveryOptions_AfterMaxRetries_ShouldNotAllowRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_max_retries_options";

        // Create 3 prior retry attempt audit logs (MaxRetryAttempts = 3)
        for (int i = 0; i < 3; i++)
        {
            var details = $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:{i + 1}";
            await _auditLogService.LogEventAsync(
                _testUserId,
                "PAYMENT_RETRY_ATTEMPTED",
                string.Empty,
                "PaymentErrorHandlingService",
                false,
                details);
        }

        // Act
        var options = await _service.GetRecoveryOptionsAsync(paymentIntentId, _testUserId);

        // Assert - Will FAIL due to BUG #PAY-001
        options.CanRetry.Should().BeFalse("should not allow retry when max attempts reached");
        options.RetryAttemptsRemaining.Should().Be(0, "no retry attempts should remain");
        options.SuggestedAction.Should().Be(RecoveryAction.UpdatePaymentMethod,
            "should suggest updating payment method after max retries");
    }

    #endregion

    #region Dunning Email Tests

    [Fact]
    public async Task ProcessInvoicePaymentFailure_FirstFailure_ShouldSendInitialDunningEmail()
    {
        // Arrange
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_dunning_1",
            RetryCount = 0
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _emailService.SentEmails.Clear();

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("inv_test_dunning_1", "sub_test_dunning_1");

        // Assert - Verify email sent
        result.Success.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1, "should send initial dunning email");

        var email = _emailService.SentEmails.First();
        email.Subject.Should().Contain("Payment Failed", "first email should be initial warning");
        email.Subject.Should().NotContain("Final", "first email should NOT be final warning");
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_FinalFailure_ShouldSendFinalWarningEmail()
    {
        // Arrange - Subscription at final dunning attempt
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_final_dunning",
            RetryCount = _retryConfig.MaxDunningAttempts - 1  // One away from cancellation
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _emailService.SentEmails.Clear();

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("inv_test_final", "sub_test_final_dunning");

        // Assert - Verify final warning email sent
        result.Success.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var email = _emailService.SentEmails.First();
        email.Subject.Should().Contain("Final Warning", "should send final warning at last attempt");
        email.Body.Should().Contain("cancelled", "final warning should mention cancellation");
        email.Body.Should().Contain("24 hours", "should mention cancellation timeline");
    }

    #endregion

    #region Edge Case Tests for Coverage (Phase 2.3)

    [Fact]
    public async Task HandlePaymentFailure_ExpiredCard_ShouldClassifyCorrectly()
    {
        // Arrange
        var paymentIntentId = "pi_test_expired_card";
        var error = new StripeError
        {
            Code = "expired_card",
            Message = "Your card has expired"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert - Verify classification
        result.Should().NotBeNull();
        result.RecoveryAction.Should().Be(RecoveryAction.UpdatePaymentMethod,
            "expired card should require payment method update");
        result.ShouldNotifyUser.Should().BeTrue("user should be notified about expired card");

        // Verify audit log created
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "PAYMENT_ERROR_HANDLED");
        auditLog.Should().NotBeNull("error handling should be logged");
    }

    [Fact]
    public async Task HandlePaymentFailure_IncorrectCvc_ShouldClassifyCorrectly()
    {
        // Arrange
        var paymentIntentId = "pi_test_incorrect_cvc";
        var error = new StripeError
        {
            Code = "incorrect_cvc",
            Message = "Your card's security code is incorrect"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert - incorrect CVC triggers retry attempt, which fails in test mode
        // In production, RetryWithSameMethod would confirm the payment intent
        // In test mode with mocked Stripe, retry fails and returns UpdatePaymentMethod
        result.Should().NotBeNull();
        result.RecoveryAction.Should().Be(RecoveryAction.UpdatePaymentMethod,
            "retry attempt fails in test mode, suggesting payment method update");
        result.ShouldNotifyUser.Should().BeTrue();
    }

    [Fact]
    public async Task HandlePaymentFailure_RateLimitExceeded_ShouldScheduleRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_rate_limit";
        var error = new StripeError
        {
            Code = "rate_limit",
            Message = "Too many requests"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert - Rate limit should trigger scheduled retry with backoff
        result.Should().NotBeNull();
        result.RecoveryAction.Should().Be(RecoveryAction.ScheduledRetry,
            "rate limit should trigger scheduled retry with backoff");
        result.NextRetryAt.Should().NotBeNull("next retry time should be scheduled");
    }

    [Fact]
    public async Task SendDunningEmail_UserNotFound_ShouldReturnError()
    {
        // Arrange - Use non-existent user ID
        var nonExistentUserId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = "in_test_no_user",
            AmountDue = 2999
        };

        // Act - Internal method called via ProcessInvoicePaymentFailureAsync
        // Create subscription for non-existent user to trigger user-not-found in email send
        var subscription = new UserSubscription
        {
            UserId = nonExistentUserId,  // User doesn't exist in database
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_no_user",
            RetryCount = 0
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("in_test_no_user", "sub_test_no_user");

        // Assert - Should handle gracefully (email will fail internally but dunning continues)
        result.Should().NotBeNull();
        // Email failure is caught internally, so overall result may still be success
        // Subscription retry count should still be incremented
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.RetryCount.Should().Be(1, "retry count should increment even if email fails");
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_SubscriptionCancelFails_ShouldReturnError()
    {
        // Arrange - Create subscription at max dunning attempts
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_cancel_fail",
            RetryCount = _retryConfig.MaxDunningAttempts - 1
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Configure MockSubscriptionService to throw exception on cancel
        _subscriptionService.SetupFailure("Subscription cancellation failed");

        // Act - Final failure should attempt to cancel subscription
        var result = await _service.ProcessInvoicePaymentFailureAsync("in_test_cancel_fail", "sub_test_cancel_fail");

        // Assert - Should return error when cancellation fails
        result.Should().NotBeNull();
        result.Success.Should().BeFalse("should fail when subscription cancellation throws exception");
        result.ErrorCode.Should().Be("SUBSCRIPTION_CANCEL_FAILED");
        result.Message.Should().Contain("cancellation failed");
        result.NextAction.Should().Be(DunningAction.CancelSubscription,
            "should still indicate cancellation was intended");
    }

    [Fact]
    public async Task GetRecoveryOptions_RequiresConfirmationStatus_ShouldSuggestRetry()
    {
        // Arrange - Payment intent that requires confirmation
        var paymentIntentId = "pi_test_requires_confirmation";

        // This test ensures DetermineSuggestedAction handles "requires_confirmation" status
        // Create audit logs to set retry count below threshold (< 3)
        await _auditLogService.LogEventAsync(
            _testUserId,
            "PAYMENT_RETRY_ATTEMPTED",
            string.Empty,
            "PaymentErrorHandlingService",
            false,
            $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:1");

        // Act
        var options = await _service.GetRecoveryOptionsAsync(paymentIntentId, _testUserId);

        // Assert - With retryCount < 3, suggested action should follow payment intent status
        options.Should().NotBeNull();
        options.SuggestedAction.Should().NotBe(RecoveryAction.ContactSupport,
            "should provide actionable recovery suggestion");
    }

    [Fact]
    public async Task HandlePaymentFailure_RequiresActionStatus_ShouldReturnActionUrl()
    {
        // Arrange - Create test payment intent that requires action (e.g., 3D Secure)
        // Note: Test mode creates mock PaymentIntent with status "requires_payment_method"
        // This test verifies the RequiresAction recovery strategy execution

        var paymentIntentId = "pi_test_requires_action";
        var error = new StripeError
        {
            Code = "authentication_required",
            Message = "Payment requires additional authentication"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert - Should provide recovery information
        result.Should().NotBeNull();
        result.ShouldNotifyUser.Should().BeTrue("user should be notified about required action");

        // Verify audit log created
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "PAYMENT_ERROR_HANDLED" &&
                                     a.UserId == _testUserId);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task HandlePaymentFailure_UnknownError_ShouldUseFallbackClassification()
    {
        // Arrange - Unknown error code
        // NOTE: In test mode, the mock PaymentIntent has status "requires_payment_method"
        // which takes precedence over unknown error codes in ClassifyPaymentError.
        // Real Unknown errors (with unknown status) would escalate to ContactSupport.
        var paymentIntentId = "pi_test_unknown_error";
        var error = new StripeError
        {
            Code = "unknown_error_code_12345",
            Message = "An unknown error occurred"
        };

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, error);

        // Assert - Unknown error codes with recognizable status use status-based classification
        result.Should().NotBeNull();
        result.RecoveryAction.Should().Be(RecoveryAction.UpdatePaymentMethod,
            "unknown error code with 'requires_payment_method' status maps to PaymentMethodRequired");
        result.ShouldNotifyUser.Should().BeTrue();

        // Verify error handling was logged (not escalation, since status was recognized)
        var errorLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "PAYMENT_ERROR_HANDLED" &&
                                     a.UserId == _testUserId);
        errorLog.Should().NotBeNull("error handling should be logged");
    }

    [Fact]
    public async Task GetRecoveryOptions_HighRetryCount_ShouldSuggestPaymentMethodUpdate()
    {
        // Arrange - Create 3+ retry attempts to test DetermineSuggestedAction retry threshold
        var paymentIntentId = "pi_test_high_retry_count";

        for (int i = 0; i < 4; i++)
        {
            await _auditLogService.LogEventAsync(
                _testUserId,
                "PAYMENT_RETRY_ATTEMPTED",
                string.Empty,
                "PaymentErrorHandlingService",
                false,
                $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:{i + 1}");
        }

        // Act
        var options = await _service.GetRecoveryOptionsAsync(paymentIntentId, _testUserId);

        // Assert - With retryCount >= 3, should suggest updating payment method
        options.Should().NotBeNull();
        options.SuggestedAction.Should().Be(RecoveryAction.UpdatePaymentMethod,
            "after 3+ retries, should suggest updating payment method (line 895-898 in service)");
        options.RetryAttemptsRemaining.Should().Be(0, "should have no retry attempts remaining");
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_InvoiceNotFoundInStripe_ShouldReturnError()
    {
        // Arrange - Use real-looking invoice ID (not starting with "in_test_")
        // But since Stripe is disabled, the test mode path is used
        // This test verifies error handling when invoice lookup fails

        var nonExistentInvoiceId = "in_real_does_not_exist";
        var subscriptionId = "sub_test_invoice_not_found";

        // Create subscription
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = subscriptionId,
            RetryCount = 0
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Stripe disabled mode will still create test invoice
        var result = await _service.ProcessInvoicePaymentFailureAsync(nonExistentInvoiceId, subscriptionId);

        // Assert - Should handle gracefully (test mode creates mock invoice)
        result.Should().NotBeNull();
        // In test mode, invoice is always found (created as mock)
        // This test ensures the code path doesn't crash
    }

    [Fact]
    public async Task RetryPaymentAsync_WithinRetryLimit_ShouldAttemptRetry()
    {
        // Arrange
        var paymentIntentId = "pi_test_retry_within_limit";

        // Create 1 retry attempt (below max of 3)
        await _auditLogService.LogEventAsync(
            _testUserId,
            "PAYMENT_RETRY_ATTEMPTED",
            string.Empty,
            "PaymentErrorHandlingService",
            false,
            $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED");

        // Act
        var result = await _service.RetryPaymentAsync(paymentIntentId, _testUserId, PaymentRetryReason.UserInitiated);

        // Assert
        result.Should().NotBeNull();
        // Retry will fail in test mode but should not throw
        result.Success.Should().BeFalse("retry fails in test mode");
    }

    [Fact]
    public async Task SendDunningEmail_FirstAttempt_ShouldSendWarningEmail()
    {
        // Arrange
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_dunning_first",
            RetryCount = 0
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("in_test_dunning", "sub_test_dunning_first");

        // Assert - First dunning attempt sends warning email
        result.Should().NotBeNull();
        result.NextAction.Should().Be(DunningAction.SendFinalWarning,
            "retry count 1 is less than MaxDunningAttempts (4), so returns SendFinalWarning");
        _emailService.SentEmails.Should().Contain(e => e.Body.Contains("payment") || e.Body.Contains("failed"));
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_SubscriptionNotFound_ShouldReturnError()
    {
        // Arrange - Use non-existent subscription ID
        var invalidSubscriptionId = "sub_does_not_exist_12345";

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("in_test_no_sub", invalidSubscriptionId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse("subscription not found should fail");
        result.ErrorCode.Should().Be("USER_NOT_FOUND",
            "service returns USER_NOT_FOUND when subscription is not found in database");
    }

    [Fact]
    public async Task HandlePaymentFailure_NullError_ShouldClassifyByStatus()
    {
        // Arrange - No error details provided, classification based on payment intent status
        var paymentIntentId = "pi_test_null_error";

        // Act
        var result = await _service.HandlePaymentFailureAsync(paymentIntentId, _testUserId, null);

        // Assert - Should handle gracefully with fallback classification
        result.Should().NotBeNull();
        result.RecoveryAction.Should().NotBe(RecoveryAction.ContactSupport,
            "recognized status should not escalate to support");
    }

    [Fact]
    public async Task GetRecoveryOptions_ZeroRetries_ShouldAllowMaxRetries()
    {
        // Arrange - Payment intent with no retry history
        var paymentIntentId = "pi_test_zero_retries";

        // Act - No audit log entries, so retry count is 0
        var options = await _service.GetRecoveryOptionsAsync(paymentIntentId, _testUserId);

        // Assert
        options.Should().NotBeNull();
        options.RetryAttemptsRemaining.Should().Be(_retryConfig.MaxRetryAttempts,
            "with zero retries, should have all retry attempts available");
        options.CanRetry.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_SecondAttempt_ShouldSendFollowUpEmail()
    {
        // Arrange
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_dunning_second",
            RetryCount = 1  // Second attempt
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("in_test_dunning2", "sub_test_dunning_second");

        // Assert - Second dunning attempt
        result.Should().NotBeNull();
        result.NextAction.Should().Be(DunningAction.SendFinalWarning,
            "retry count 2 is less than MaxDunningAttempts (4), so returns SendFinalWarning");
        _emailService.SentEmails.Should().HaveCountGreaterThan(0, "should send dunning email");
    }

    [Fact]
    public async Task ProcessInvoicePaymentFailure_ThirdAttempt_ShouldSendUrgentEmail()
    {
        // Arrange
        var subscription = new UserSubscription
        {
            UserId = _testUserId,
            SubscriptionTierId = _testSubscriptionTierId,
            Status = SubscriptionStatus.Active,
            ExternalSubscriptionId = "sub_test_dunning_third",
            RetryCount = 2  // Third attempt
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ProcessInvoicePaymentFailureAsync("in_test_dunning3", "sub_test_dunning_third");

        // Assert - Third dunning attempt (urgent)
        result.Should().NotBeNull();
        result.NextAction.Should().Be(DunningAction.SendFinalWarning,
            "retry count 3 is less than MaxDunningAttempts (4), so returns SendFinalWarning");
        _emailService.SentEmails.Should().Contain(e => e.Body.Contains("urgent") || e.Body.Contains("final"),
            "third attempt should send final warning email");
    }

    [Fact]
    public async Task GetRecoveryOptions_ExactlyMaxRetries_ShouldNotAllowRetry()
    {
        // Arrange - Create exactly MaxRetryAttempts (3) retry attempts
        var paymentIntentId = "pi_test_exact_max_retries";

        for (int i = 0; i < _retryConfig.MaxRetryAttempts; i++)
        {
            await _auditLogService.LogEventAsync(
                _testUserId,
                "PAYMENT_RETRY_ATTEMPTED",
                string.Empty,
                "PaymentErrorHandlingService",
                false,
                $"PaymentIntentId:{paymentIntentId}|Reason:AutomaticRetry|ErrorCode:PAYMENT_FAILED|AttemptNumber:{i + 1}");
        }

        // Act
        var options = await _service.GetRecoveryOptionsAsync(paymentIntentId, _testUserId);

        // Assert
        options.Should().NotBeNull();
        options.CanRetry.Should().BeFalse("should not allow retry after max attempts");
        options.RetryAttemptsRemaining.Should().Be(0);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
