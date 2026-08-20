using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for SubscriptionBillingService.
///
/// These tests follow our testing philosophy:
/// - Real database (in-memory) for data persistence
/// - Real internal services where possible
/// - Only mock EXTERNAL services (payment, email)
/// - Verify actual database state changes, not just mock calls
/// </summary>
[Collection("Integration Services 3")]
[IntegrationTest]
[FinancialTest]
public class SubscriptionBillingServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockPaymentService _mockPaymentService;
    private readonly Mocks.MockEmailService _mockEmailService;
    private readonly MockAuditLogService _auditLogService;
    private readonly MockSubscriptionService _subscriptionService;
    private readonly SubscriptionBillingService _billingService;

    // Test data
    private User _testUser = null!;
    private SubscriptionTier _testTier = null!;
    private PaymentMethod _testPaymentMethod = null!;

    public SubscriptionBillingServiceTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"SubscriptionBillingTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        // External services - OK to mock
        _mockPaymentService = new MockPaymentService();
        _mockEmailService = new Mocks.MockEmailService();

        // Internal service - uses real context for audit logging
        _auditLogService = new MockAuditLogService(_context);

        // BUG-026 FIX: Replace Moq.Mock<ISubscriptionService> with MockSubscriptionService.
        // ISubscriptionService must not be mocked with Moq (internal service rule). MockSubscriptionService
        // is the approved test double — it persists outcomes to the real in-memory database, so tests
        // can verify actual DB state changes, not just mock interactions.
        _subscriptionService = new MockSubscriptionService(_context);

        _billingService = new SubscriptionBillingService(
            _context,
            _subscriptionService,
            _mockPaymentService,
            _mockEmailService,
            _auditLogService,
            new LoggerFactory().CreateLogger<SubscriptionBillingService>()
        );

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(_testUser);

        // Create subscription tier
        _testTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            Price = 29.99m,
            AnnualPrice = 299.99m,
            IsActive = true,
            CreditBonus = 1000,
            MaxActiveProjects = 10,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTiers.Add(_testTier);

        // Create payment method (uses correct property names)
        _testPaymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_123",
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryDate = "12/2030",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.PaymentMethods.Add(_testPaymentMethod);

        _context.SaveChanges();

        // Add payment method to mock for GetUserPaymentMethodsAsync
        _mockPaymentService.AddPaymentMethodForUser(_testUser.Id, _testPaymentMethod);
    }

    #region ProcessDueRenewals Tests

    [Fact]
    public async Task ProcessDueRenewals_WithActiveSubscriptionDue_ShouldProcessPaymentAndRenew()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(-1), // Past due
            autoRenew: true);

        _mockPaymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert - Verify result metrics
        result.TotalProcessed.Should().Be(1);
        result.SuccessfulRenewals.Should().Be(1);
        result.FailedRenewals.Should().Be(0);
        result.TotalRevenue.Should().Be(_testTier.Price);
        result.ProcessedSubscriptionIds.Should().Contain(subscription.Id);

        // Assert - Verify REAL database state change
        var updatedSubscription = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == subscription.Id);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Active);
        updatedSubscription.NextBillingDate.Should().BeAfter(DateTime.UtcNow);

        // Assert - Verify payment was processed (external service call)
        _mockPaymentService.ProcessedPayments.Should().ContainSingle(p =>
            p.SubscriptionId == subscription.Id &&
            p.Amount == _testTier.Price &&
            p.Success);
    }

    [Fact]
    public async Task ProcessDueRenewals_WithAnnualSubscription_ShouldChargeAnnualPrice()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(-1),
            autoRenew: true,
            isAnnual: true);

        _mockPaymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert - Verify annual price was charged
        result.TotalRevenue.Should().Be(_testTier.AnnualPrice);
        _mockPaymentService.ProcessedPayments.Should().ContainSingle(p =>
            p.Amount == _testTier.AnnualPrice);
    }

    [Fact]
    public async Task ProcessDueRenewals_WhenPaymentFails_ShouldMarkPastDueAndScheduleRetry()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(-1),
            autoRenew: true);

        _mockPaymentService.SetupFailure("Card declined");

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert - Verify result
        result.FailedRenewals.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("Card declined"));

        // Assert - Verify REAL database state change
        var updatedSubscription = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == subscription.Id);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.PastDue);
        updatedSubscription.RetryCount.Should().Be(1);
        updatedSubscription.NextRetryAt.Should().NotBeNull();
        updatedSubscription.NextRetryAt.Should().BeAfter(DateTime.UtcNow);

        // Assert - Verify audit log was created (internal service writes to DB)
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id && a.Action == "SUBSCRIPTION_RENEWAL_FAILED");
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeFalse();

        // Assert - Verify email notification was sent (external service)
        _mockEmailService.SentEmails.Should().ContainSingle(e =>
            e.ToEmail == _testUser.Email &&
            e.Subject.Contains("Payment Failed"));
    }

    [Fact]
    public async Task ProcessDueRenewals_WithNoSubscriptionsDue_ShouldReturnEmptyResult()
    {
        // Arrange - Create subscription NOT due yet
        await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(30), // Future date
            autoRenew: true);

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.TotalProcessed.Should().Be(0);
        result.SuccessfulRenewals.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
        _mockPaymentService.ProcessedPayments.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRenewals_WithAutoRenewDisabled_ShouldNotProcess()
    {
        // Arrange
        await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(-1),
            autoRenew: false); // Auto-renew OFF

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.TotalProcessed.Should().Be(0);
        _mockPaymentService.ProcessedPayments.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDueRenewals_WithCancelledSubscription_ShouldNotProcess()
    {
        // Arrange
        await CreateTestSubscription(
            status: SubscriptionStatus.Cancelled,
            nextBillingDate: DateTime.UtcNow.AddDays(-1),
            autoRenew: true);

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.TotalProcessed.Should().Be(0);
    }

    #endregion

    #region ProcessExpiringTrials Tests

    [Fact]
    public async Task ProcessExpiringTrials_WithPaymentMethod_ShouldConvertToPaid()
    {
        // Arrange
        var trial = await CreateTestSubscription(
            status: SubscriptionStatus.Trial,
            nextBillingDate: DateTime.UtcNow.AddDays(30),
            autoRenew: true,
            trialEndDate: DateTime.UtcNow.AddHours(-1)); // Trial just expired

        // Act
        var result = await _billingService.ProcessExpiringTrialsAsync();

        // Assert
        result.TrialsProcessed.Should().Be(1);
        result.SuccessfulConversions.Should().Be(1);
        result.ConvertedSubscriptionIds.Should().Contain(trial.Id);

        // Assert - Verify REAL database state
        var updatedTrial = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == trial.Id);
        updatedTrial.Status.Should().Be(SubscriptionStatus.Active);

        // Assert - Verify email was sent
        _mockEmailService.SentEmails.Should().Contain(e =>
            e.Subject.Contains("Trial Converted"));
    }

    [Fact]
    public async Task ProcessExpiringTrials_WithoutPaymentMethod_ShouldCancelTrial()
    {
        // Arrange - Create user without payment method
        var userWithoutPayment = new User
        {
            Id = Guid.NewGuid(),
            Email = "nopayment@example.com",
            UserName = "nopayment@example.com",
            FirstName = "No",
            LastName = "Payment",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(userWithoutPayment);
        await _context.SaveChangesAsync();

        // Note: Don't add payment method to mock for this user
        var trial = await CreateTestSubscription(
            status: SubscriptionStatus.Trial,
            nextBillingDate: DateTime.UtcNow.AddDays(30),
            autoRenew: true,
            trialEndDate: DateTime.UtcNow.AddHours(-1),
            userId: userWithoutPayment.Id);

        // Act
        var result = await _billingService.ProcessExpiringTrialsAsync();

        // Assert
        result.TrialsProcessed.Should().Be(1);
        result.FailedConversions.Should().Be(1);
        result.TrialsCancelled.Should().Be(1);

        // Assert - Verify REAL database state
        var updatedTrial = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == trial.Id);
        updatedTrial.Status.Should().Be(SubscriptionStatus.Cancelled);
        updatedTrial.CancellationReason.Should().Contain("no payment method");

        // Assert - Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == userWithoutPayment.Id && a.Action == "TRIAL_EXPIRED");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region ProcessFailedPaymentRetries Tests

    [Fact]
    public async Task ProcessFailedPaymentRetries_WhenRetrySucceeds_ShouldReactivateSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.PastDue,
            nextBillingDate: DateTime.UtcNow.AddDays(-5),
            autoRenew: true,
            retryCount: 1,
            nextRetryAt: DateTime.UtcNow.AddHours(-1)); // Ready for retry

        _mockPaymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessFailedPaymentRetriesAsync(maxRetries: 3);

        // Assert
        result.RetriesAttempted.Should().Be(1);
        result.SuccessfulRetries.Should().Be(1);
        result.RevenueRecovered.Should().Be(_testTier.Price);

        // Assert - Verify REAL database state
        var updatedSubscription = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == subscription.Id);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Active);

        // Assert - Verify success email sent
        _mockEmailService.SentEmails.Should().Contain(e =>
            e.Subject.Contains("Payment Successful"));
    }

    [Fact]
    public async Task ProcessFailedPaymentRetries_WhenMaxRetriesReached_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.PastDue,
            nextBillingDate: DateTime.UtcNow.AddDays(-10),
            autoRenew: true,
            retryCount: 2, // Already 2 retries
            nextRetryAt: DateTime.UtcNow.AddHours(-1));

        _mockPaymentService.SetupFailure("Card expired");

        // Act
        var result = await _billingService.ProcessFailedPaymentRetriesAsync(maxRetries: 3);

        // Assert
        result.RetriesAttempted.Should().Be(1);
        result.FailedRetries.Should().Be(1);
        result.SubscriptionsCancelled.Should().Be(1);

        // Assert - Verify REAL database state
        var updatedSubscription = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == subscription.Id);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        updatedSubscription.CancellationReason.Should().Contain("3 retry attempts");

        // Assert - Verify cancellation email sent
        _mockEmailService.SentEmails.Should().Contain(e =>
            e.Subject.Contains("Subscription Cancelled"));
    }

    #endregion

    #region ProcessPastDueCancellations Tests

    [Fact]
    public async Task ProcessPastDueCancellations_AfterGracePeriod_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.PastDue,
            nextBillingDate: DateTime.UtcNow.AddDays(-20),
            autoRenew: true);

        // Set UpdatedAt to be past grace period
        subscription.UpdatedAt = DateTime.UtcNow.AddDays(-10);
        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ProcessPastDueCancellationsAsync(gracePeriodDays: 7);

        // Assert
        result.SubscriptionsCancelled.Should().Be(1);
        result.CancelledSubscriptionIds.Should().Contain(subscription.Id);

        // Assert - Verify REAL database state
        var updatedSubscription = await _context.UserSubscriptions
            .FirstAsync(s => s.Id == subscription.Id);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        updatedSubscription.CancelledAt.Should().NotBeNull();

        // Assert - Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "SUBSCRIPTION_CANCELLED_PAST_DUE");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region ValidateActiveSubscriptions Tests

    [Fact]
    public async Task ValidateActiveSubscriptions_WithValidSubscription_ShouldPass()
    {
        // Arrange
        await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(10),
            autoRenew: true);

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.TotalValidated.Should().Be(1);
        result.ValidSubscriptions.Should().Be(1);
        result.InvalidSubscriptions.Should().Be(0);
    }

    [Fact]
    public async Task ValidateActiveSubscriptions_WithPastBillingDate_ShouldFlagAsInvalid()
    {
        // Arrange
        var subscription = await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(-5), // In the past
            autoRenew: true);

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.InvalidSubscriptions.Should().Be(1);
        result.ValidationIssues.Should().Contain(i => i.Contains("billing date is in the past"));
        result.ProblematicSubscriptionIds.Should().Contain(subscription.Id);
    }

    [Fact]
    public async Task ValidateActiveSubscriptions_TrialWithoutEndDate_ShouldFlagAsInvalid()
    {
        // Arrange
        var trial = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            User = _testUser,
            SubscriptionTierId = _testTier.Id,
            SubscriptionTier = _testTier,
            Status = SubscriptionStatus.Trial,
            NextBillingDate = DateTime.UtcNow.AddDays(30),
            AutoRenew = true,
            TrialEndDate = null, // Missing end date
            PaymentMethodId = _testPaymentMethod.Id,
            PaymentMethod = _testPaymentMethod,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.UserSubscriptions.AddAsync(trial);
        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.InvalidSubscriptions.Should().Be(1);
        result.ValidationIssues.Should().Contain(i => i.Contains("no end date"));
    }

    #endregion

    #region SendBillingReminders Tests

    [Fact]
    public async Task SendBillingReminders_WithUpcomingRenewal_ShouldSendReminder()
    {
        // Arrange
        await CreateTestSubscription(
            status: SubscriptionStatus.Active,
            nextBillingDate: DateTime.UtcNow.AddDays(2), // Renews in 2 days
            autoRenew: true);

        // Act
        var result = await _billingService.SendBillingRemindersAsync(daysBefore: 3);

        // Assert
        result.RemindersSent.Should().Be(1);
        result.UsersNotified.Should().Be(1);

        // Assert - Verify email was sent
        _mockEmailService.SentEmails.Should().ContainSingle(e =>
            e.Subject.Contains("Upcoming Subscription Renewal") &&
            e.Body != null && e.Body.Contains("$29.99"));
    }

    #endregion

    #region Helper Methods

    private async Task<UserSubscription> CreateTestSubscription(
        SubscriptionStatus status,
        DateTime nextBillingDate,
        bool autoRenew,
        bool isAnnual = false,
        DateTime? trialEndDate = null,
        int retryCount = 0,
        DateTime? nextRetryAt = null,
        Guid? userId = null)
    {
        var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) ?? _testUser : _testUser;
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? _testUser.Id,
            User = user,
            SubscriptionTierId = _testTier.Id,
            SubscriptionTier = _testTier,
            Status = status,
            NextBillingDate = nextBillingDate,
            AutoRenew = autoRenew,
            IsAnnual = isAnnual,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            PaymentMethodId = userId.HasValue ? null : _testPaymentMethod.Id,
            PaymentMethod = userId.HasValue ? null : _testPaymentMethod,
            RetryCount = retryCount,
            NextRetryAt = nextRetryAt,
            TrialEndDate = trialEndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.UserSubscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();

        return subscription;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
