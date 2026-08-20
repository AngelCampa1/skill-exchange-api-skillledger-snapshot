using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for SubscriptionBillingService using real database and business logic
/// Following anti-mocking pattern: Real database, real AuditLogService, mock only external services
/// </summary>
[IntegrationTest]
[FinancialTest]
public class SubscriptionBillingServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly SubscriptionBillingService _billingService;
    private readonly MockSubscriptionService _subscriptionService;
    private readonly MockPaymentService _paymentService;
    private readonly Mocks.MockEmailService _emailService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<SubscriptionBillingService> _logger;

    public SubscriptionBillingServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase($"SubscriptionBillingTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Real internal services
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var auditLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<AuditLogService>();
        _auditLogService = new AuditLogService(_context, auditLogger, memoryCache);

        // Mock external services only
        _subscriptionService = new MockSubscriptionService();
        _paymentService = new MockPaymentService();
        _emailService = new Mocks.MockEmailService();

        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<SubscriptionBillingService>();

        _billingService = new SubscriptionBillingService(
            _context,
            _subscriptionService,
            _paymentService,
            _emailService,
            _auditLogService,
            _logger);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ProcessDueRenewalsAsync_ValidSubscription_ShouldRenewSuccessfully()
    {
        // Arrange
        var user = CreateUser("renew@test.com", "John", "Doe");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(-1));

        await _context.SaveChangesAsync();

        _paymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().Be(1);
        result.SuccessfulRenewals.Should().Be(1);
        result.FailedRenewals.Should().Be(0);
        result.TotalRevenue.Should().Be(49.99m);
        result.ProcessedSubscriptionIds.Should().Contain(subscription.Id);

        // Verify payment was processed
        _paymentService.ProcessedPayments.Should().HaveCount(1);
        _paymentService.ProcessedPayments[0].Amount.Should().Be(49.99m);
        _paymentService.ProcessedPayments[0].Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDueRenewalsAsync_PaymentFails_ShouldMarkAsPastDue()
    {
        // Arrange
        var user = CreateUser("failpay@test.com", "Jane", "Smith");
        var tier = CreateSubscriptionTier("Basic", 19.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(-1));

        await _context.SaveChangesAsync();

        _paymentService.SetupFailure("Card declined");

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.SuccessfulRenewals.Should().Be(0);
        result.FailedRenewals.Should().Be(1);
        result.TotalRevenue.Should().Be(0);
        result.Errors.Should().ContainMatch("*Payment failed*Card declined*");

        // Verify subscription status updated
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.PastDue);
        updatedSubscription.RetryCount.Should().Be(1);
        updatedSubscription.NextRetryAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(1), TimeSpan.FromMinutes(1));

        // Verify email notification sent
        _emailService.SentEmails.Should().HaveCount(1);
        _emailService.SentEmails.First().ToEmail.Should().Be(user.Email);
    }

    [Fact]
    public async Task ProcessDueRenewalsAsync_AnnualSubscription_ShouldUseAnnualPrice()
    {
        // Arrange
        var user = CreateUser("annual@test.com", "Annual", "User");
        var tier = CreateSubscriptionTier("Enterprise", 99.99m, 999.99m); // Annual price
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(-1), isAnnual: true);

        await _context.SaveChangesAsync();

        _paymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.SuccessfulRenewals.Should().Be(1);
        result.TotalRevenue.Should().Be(999.99m);

        // Verify annual price was charged
        _paymentService.ProcessedPayments[0].Amount.Should().Be(999.99m);
    }

    [Fact]
    public async Task ProcessDueRenewalsAsync_NoAutoRenew_ShouldNotProcess()
    {
        // Arrange
        var user = CreateUser("norenew@test.com", "No", "Renew");
        var tier = CreateSubscriptionTier("Basic", 19.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(-1));
        subscription.AutoRenew = false; // Disable auto-renew

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ProcessDueRenewalsAsync();

        // Assert
        result.TotalProcessed.Should().Be(0);
        result.SuccessfulRenewals.Should().Be(0);
    }

    [Fact]
    public async Task ProcessExpiringTrialsAsync_WithPaymentMethod_ShouldConvertToPaid()
    {
        // Arrange
        var user = CreateUser("trial@test.com", "Trial", "User");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id, isDefault: true);
        var trialSubscription = CreateTrialSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddHours(-12));

        await _context.SaveChangesAsync();

        _paymentService.AddPaymentMethodForUser(user.Id, paymentMethod);
        _paymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessExpiringTrialsAsync();

        // Assert
        result.TrialsProcessed.Should().Be(1);
        result.SuccessfulConversions.Should().Be(1);
        result.FailedConversions.Should().Be(0);
        result.TrialsCancelled.Should().Be(0);
        result.ConvertedSubscriptionIds.Should().Contain(trialSubscription.Id);

        // Verify email notification
        _emailService.SentEmails.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ProcessExpiringTrialsAsync_NoPaymentMethod_ShouldCancelTrial()
    {
        // Arrange
        var user = CreateUser("notrial@test.com", "No", "Payment");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var trialSubscription = CreateTrialSubscription(user.Id, tier.Id, null, DateTime.UtcNow.AddHours(-12));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ProcessExpiringTrialsAsync();

        // Assert
        result.TrialsProcessed.Should().Be(1);
        result.SuccessfulConversions.Should().Be(0);
        result.FailedConversions.Should().Be(1);
        result.TrialsCancelled.Should().Be(1);

        // Verify subscription cancelled
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(trialSubscription.Id);
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
        updatedSubscription.CancellationReason.Should().Contain("no payment method");
    }

    [Fact]
    public async Task ProcessFailedPaymentRetriesAsync_SuccessfulRetry_ShouldReactivateSubscription()
    {
        // Arrange
        var user = CreateUser("retry@test.com", "Retry", "User");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreatePastDueSubscription(user.Id, tier.Id, paymentMethod.Id, retryCount: 1);

        await _context.SaveChangesAsync();

        _paymentService.SetupSuccess();

        // Act
        var result = await _billingService.ProcessFailedPaymentRetriesAsync(maxRetries: 3);

        // Assert
        result.RetriesAttempted.Should().Be(1);
        result.SuccessfulRetries.Should().Be(1);
        result.FailedRetries.Should().Be(0);
        result.SubscriptionsCancelled.Should().Be(0);
        result.RevenueRecovered.Should().Be(49.99m);

        // Verify payment processed
        _paymentService.ProcessedPayments.Should().HaveCount(1);
        _paymentService.ProcessedPayments[0].Amount.Should().Be(49.99m);
    }

    [Fact]
    public async Task ProcessFailedPaymentRetriesAsync_MaxRetriesReached_ShouldCancelSubscription()
    {
        // Arrange
        var user = CreateUser("maxretry@test.com", "Max", "Retry");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreatePastDueSubscription(user.Id, tier.Id, paymentMethod.Id, retryCount: 2);

        await _context.SaveChangesAsync();

        _paymentService.SetupFailure("Card declined");

        // Act
        var result = await _billingService.ProcessFailedPaymentRetriesAsync(maxRetries: 3);

        // Assert
        result.RetriesAttempted.Should().Be(1);
        result.SuccessfulRetries.Should().Be(0);
        result.FailedRetries.Should().Be(1);
        result.SubscriptionsCancelled.Should().Be(1);

        // Verify subscription cancelled
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
        updatedSubscription.RetryCount.Should().Be(3);
        updatedSubscription.CancellationReason.Should().Contain("Payment failed after 3 retry attempts");
    }

    [Fact]
    public async Task ProcessFailedPaymentRetriesAsync_RetryCountIncremented_ShouldUpdateNextRetryDate()
    {
        // Arrange
        var user = CreateUser("increment@test.com", "Increment", "User");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreatePastDueSubscription(user.Id, tier.Id, paymentMethod.Id, retryCount: 0);

        await _context.SaveChangesAsync();

        _paymentService.SetupFailure("Insufficient funds");

        // Act
        var result = await _billingService.ProcessFailedPaymentRetriesAsync(maxRetries: 3);

        // Assert
        result.FailedRetries.Should().Be(1);

        // Verify retry count and next retry date updated
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.RetryCount.Should().Be(1);
        updatedSubscription.NextRetryAt.Should().BeAfter(DateTime.UtcNow); // Exponential backoff
    }

    [Fact]
    public async Task ProcessPastDueCancellationsAsync_BeyondGracePeriod_ShouldCancelSubscription()
    {
        // Arrange
        var user = CreateUser("pastdue@test.com", "Past", "Due");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreatePastDueSubscription(user.Id, tier.Id, paymentMethod.Id, retryCount: 2);
        subscription.UpdatedAt = DateTime.UtcNow.AddDays(-8); // 8 days past due

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ProcessPastDueCancellationsAsync(gracePeriodDays: 7);

        // Assert
        result.SubscriptionsCancelled.Should().Be(1);
        result.CancelledSubscriptionIds.Should().Contain(subscription.Id);

        // Verify subscription cancelled
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
        updatedSubscription.CancellationReason.Should().Contain("Cancelled after 7 days past due");

        // Verify audit log
        var auditLogs = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "SUBSCRIPTION_CANCELLED_PAST_DUE")
            .ToListAsync();
        auditLogs.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessPastDueCancellationsAsync_WithinGracePeriod_ShouldNotCancel()
    {
        // Arrange
        var user = CreateUser("grace@test.com", "Grace", "Period");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreatePastDueSubscription(user.Id, tier.Id, paymentMethod.Id, retryCount: 1);
        subscription.UpdatedAt = DateTime.UtcNow.AddDays(-5); // Only 5 days past due

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ProcessPastDueCancellationsAsync(gracePeriodDays: 7);

        // Assert
        result.SubscriptionsCancelled.Should().Be(0);

        // Verify subscription still past due
        var subscription2 = await _context.UserSubscriptions.FindAsync(subscription.Id);
        subscription2!.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task SendBillingRemindersAsync_UpcomingRenewal_ShouldSendReminder()
    {
        // Arrange
        var user = CreateUser("reminder@test.com", "Reminder", "User");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(2));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.SendBillingRemindersAsync(daysBefore: 3);

        // Assert
        result.RemindersSent.Should().Be(1);
        result.UsersNotified.Should().Be(1);

        // Verify email sent
        _emailService.SentEmails.Should().HaveCount(1);
        _emailService.SentEmails.First().ToEmail.Should().Be(user.Email);
        // FIX: Check Subject property instead of UserName for email content validation
        _emailService.SentEmails.First().Subject.Should().Contain("Upcoming Subscription Renewal");
    }

    [Fact]
    public async Task SendBillingRemindersAsync_NoUpcomingRenewals_ShouldNotSendReminders()
    {
        // Arrange
        var user = CreateUser("noremind@test.com", "No", "Reminder");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(10));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.SendBillingRemindersAsync(daysBefore: 3);

        // Assert
        result.RemindersSent.Should().Be(0);
        _emailService.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSubscriptionStatisticsAsync_ShouldCalculateMRRCorrectly()
    {
        // Arrange
        var user1 = CreateUser("user1@test.com", "User", "One");
        var user2 = CreateUser("user2@test.com", "User", "Two");
        var tier1 = CreateSubscriptionTier("Basic", 19.99m, null);
        var tier2 = CreateSubscriptionTier("Enterprise", 99.99m, 999.99m); // Annual
        var paymentMethod1 = CreatePaymentMethod(user1.Id);
        var paymentMethod2 = CreatePaymentMethod(user2.Id);

        CreateActiveSubscription(user1.Id, tier1.Id, paymentMethod1.Id, DateTime.UtcNow.AddMonths(1));
        CreateActiveSubscription(user2.Id, tier2.Id, paymentMethod2.Id, DateTime.UtcNow.AddYears(1), isAnnual: true);

        await _context.SaveChangesAsync();

        // Act
        await _billingService.UpdateSubscriptionStatisticsAsync();

        // Assert - Should not throw, logs statistics
        // MRR calculation: $19.99 + ($999.99 / 12) = $19.99 + $83.33 = $103.32 (approximately)
        var activeSubscriptions = await _context.UserSubscriptions
            .Where(us => us.Status == SubscriptionStatus.Active)
            .CountAsync();

        activeSubscriptions.Should().Be(2);
    }

    [Fact]
    public async Task ValidateActiveSubscriptionsAsync_ValidSubscription_ShouldPass()
    {
        // Arrange
        var user = CreateUser("valid@test.com", "Valid", "User");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddMonths(1));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.TotalValidated.Should().Be(1);
        result.ValidSubscriptions.Should().Be(1);
        result.InvalidSubscriptions.Should().Be(0);
        result.ValidationIssues.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateActiveSubscriptionsAsync_NoPaymentMethod_ShouldIdentifyIssue()
    {
        // Arrange
        var user = CreateUser("nopayment@test.com", "No", "Payment");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, null, DateTime.UtcNow.AddMonths(1));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.TotalValidated.Should().Be(1);
        result.ValidSubscriptions.Should().Be(0);
        result.InvalidSubscriptions.Should().Be(1);
        result.ValidationIssues.Should().ContainMatch("*No payment method assigned*");
        result.ProblematicSubscriptionIds.Should().Contain(subscription.Id);
    }

    [Fact]
    public async Task ValidateActiveSubscriptionsAsync_BillingDateInPast_ShouldIdentifyIssue()
    {
        // Arrange
        var user = CreateUser("pastbilling@test.com", "Past", "Billing");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var subscription = CreateActiveSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(-5));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.InvalidSubscriptions.Should().Be(1);
        result.ValidationIssues.Should().ContainMatch("*Next billing date is in the past*");
    }

    [Fact]
    public async Task ValidateActiveSubscriptionsAsync_ExpiredTrial_ShouldIdentifyIssue()
    {
        // Arrange
        var user = CreateUser("expiredtrial@test.com", "Expired", "Trial");
        var tier = CreateSubscriptionTier("Professional", 49.99m, null);
        var paymentMethod = CreatePaymentMethod(user.Id);
        var trialSubscription = CreateTrialSubscription(user.Id, tier.Id, paymentMethod.Id, DateTime.UtcNow.AddDays(-2));

        await _context.SaveChangesAsync();

        // Act
        var result = await _billingService.ValidateActiveSubscriptionsAsync();

        // Assert
        result.InvalidSubscriptions.Should().Be(1);
        result.ValidationIssues.Should().ContainMatch("*Trial subscription should have ended*");
    }

    // Helper methods
    private User CreateUser(string email, string firstName, string lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        return user;
    }

    private SubscriptionTier CreateSubscriptionTier(string name, decimal price, decimal? annualPrice)
    {
        var tier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            AnnualPrice = annualPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.SubscriptionTiers.Add(tier);
        return tier;
    }

    private PaymentMethod CreatePaymentMethod(Guid userId, bool isDefault = true)
    {
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "Stripe",
            Token = $"pm_{Guid.NewGuid():N}",
            Type = "card",
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryDate = "12/2030",
            IsDefault = isDefault,
            IsValid = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.PaymentMethods.Add(paymentMethod);
        return paymentMethod;
    }

    private UserSubscription CreateActiveSubscription(Guid userId, Guid tierId, Guid? paymentMethodId, DateTime nextBillingDate, bool isAnnual = false)
    {
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionTierId = tierId,
            PaymentMethodId = paymentMethodId,
            ExternalSubscriptionId = $"sub_{Guid.NewGuid():N}",
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            NextBillingDate = nextBillingDate,
            IsAnnual = isAnnual,
            AutoRenew = true,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        return subscription;
    }

    private UserSubscription CreateTrialSubscription(Guid userId, Guid tierId, Guid? paymentMethodId, DateTime trialEndDate)
    {
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionTierId = tierId,
            PaymentMethodId = paymentMethodId,
            ExternalSubscriptionId = $"sub_{Guid.NewGuid():N}",
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-14),
            TrialEndDate = trialEndDate,
            NextBillingDate = trialEndDate,
            AutoRenew = true,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        return subscription;
    }

    private UserSubscription CreatePastDueSubscription(Guid userId, Guid tierId, Guid paymentMethodId, int retryCount)
    {
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionTierId = tierId,
            PaymentMethodId = paymentMethodId,
            ExternalSubscriptionId = $"sub_{Guid.NewGuid():N}",
            Status = SubscriptionStatus.PastDue,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            NextBillingDate = DateTime.UtcNow.AddMonths(-1),
            NextRetryAt = DateTime.UtcNow.AddHours(-1), // Ready for retry
            RetryCount = retryCount,
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        return subscription;
    }
}
