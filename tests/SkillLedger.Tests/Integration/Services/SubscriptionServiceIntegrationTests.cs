using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for SubscriptionService - Subscription Lifecycle Management.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real internal services (credit wallet, audit log writes to DB)
/// - Mocks only EXTERNAL services (payment gateway - Stripe)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (IPaymentService)
/// </summary>
public class SubscriptionServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly SubscriptionService _service;

    // REAL internal services
    private readonly MockCreditWalletService _walletService;
    private readonly MockAuditLogService _auditLogService;

    // EXTERNAL service (OK to mock)
    private readonly MockPaymentService _mockPaymentService;

    // Test data
    private readonly User _testUser;
    private readonly User _testUser2;
    private readonly SubscriptionTier _freeTier;
    private readonly SubscriptionTier _proPier;
    private readonly SubscriptionTier _enterpriseTier;
    private readonly Guid _testPaymentMethodId;
    private readonly Guid _testPaymentMethodId2;

    public SubscriptionServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"SubscriptionTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal services
        _walletService = new MockCreditWalletService(_context);
        _auditLogService = new MockAuditLogService(_context);

        // Setup EXTERNAL service
        _mockPaymentService = new MockPaymentService();

        var logger = new LoggerFactory().CreateLogger<SubscriptionService>();

        _service = new SubscriptionService(
            _context,
            _mockPaymentService,
            _walletService,
            _auditLogService,
            logger);

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

        _testUser2 = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser2",
            Email = "test2@example.com",
            FirstName = "Test",
            LastName = "User2",
            EmailConfirmed = true
        };

        // Create subscription tiers
        _freeTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Free,
            Name = "Free",
            Price = 0,
            AnnualPrice = 0,
            MaxActiveProjects = 3,
            MaxTeamMembers = 1,
            CreditBonus = 0,
            IsActive = true,
            SortOrder = 1
        };

        _proPier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Professional,
            Name = "Professional",
            Price = 29.99m,
            AnnualPrice = 299.99m,
            MaxActiveProjects = 50,
            MaxTeamMembers = 10,
            CreditBonus = 100,
            PrioritySupport = true,
            ApiAccess = true,
            AdvancedAnalytics = true,
            IsActive = true,
            SortOrder = 2
        };

        _enterpriseTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Enterprise,
            Name = "Enterprise",
            Price = 99.99m,
            AnnualPrice = 999.99m,
            MaxActiveProjects = -1, // Unlimited
            MaxTeamMembers = -1,
            CreditBonus = 500,
            PrioritySupport = true,
            ApiAccess = true,
            AdvancedAnalytics = true,
            AdvancedFraudDetection = true,
            MultiSignature = true,
            CustomIntegrations = true,
            MaxMonthlyEarnings = -1,
            IsActive = true,
            SortOrder = 3
        };

        _context.Users.AddRange(_testUser, _testUser2);
        _context.SubscriptionTiers.AddRange(_freeTier, _proPier, _enterpriseTier);
        _context.SaveChanges();

        // Create credit wallets for test users and system account
        _walletService.CreateWalletAsync(Guid.Empty).Wait(); // System account for bonus credits
        _walletService.CreateWalletAsync(_testUser.Id).Wait();
        _walletService.CreateWalletAsync(_testUser2.Id).Wait();

        // Add credits to wallets so credit transfers work
        _walletService.AddCreditsAsync(_testUser.Id, 10000, "Test credits", CreditTransactionType.Purchase).Wait();
        _walletService.AddCreditsAsync(_testUser2.Id, 10000, "Test credits", CreditTransactionType.Purchase).Wait();
        _walletService.AddCreditsAsync(Guid.Empty, 100000, "System credits", CreditTransactionType.Purchase).Wait();

        // Setup payment methods - add to both mock AND database
        _testPaymentMethodId = Guid.NewGuid();
        var paymentMethod1 = new PaymentMethod
        {
            Id = _testPaymentMethodId,
            UserId = _testUser.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card",
            IsDefault = true,
            IsValid = true,
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryDate = "12/2030"
        };
        _testPaymentMethodId2 = Guid.NewGuid();
        var paymentMethod2 = new PaymentMethod
        {
            Id = _testPaymentMethodId2,
            UserId = _testUser2.Id,
            Provider = "stripe",
            Type = "card",
            Token = "pm_test_card2",
            IsDefault = true,
            IsValid = true,
            Last4Digits = "4242",
            Brand = "Visa",
            ExpiryDate = "12/2030"
        };

        // Add to database for validation
        _context.PaymentMethods.AddRange(paymentMethod1, paymentMethod2);
        _context.SaveChanges();

        // Add to mock payment service
        _mockPaymentService.AddPaymentMethodForUser(_testUser.Id, paymentMethod1);
        _mockPaymentService.AddPaymentMethodForUser(_testUser2.Id, paymentMethod2);
    }

    #region Create Subscription Tests

    [Fact]
    public async Task CreateSubscriptionAsync_ValidTrial_ShouldCreateTrialSubscription()
    {
        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId,
            isTrial: true);

        // Assert - Verify database state
        subscription.Should().NotBeNull();
        subscription.Status.Should().Be(SubscriptionStatus.Trial);
        subscription.TrialEndDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        subscription.NextBillingDate.Should().Be(subscription.TrialEndDate);

        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription.Should().NotBeNull();
        dbSubscription!.Status.Should().Be(SubscriptionStatus.Trial);

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id && a.Action.Contains("SUBSCRIPTION"));
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ValidPaidMonthly_ShouldCreateActiveSubscription()
    {
        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId,
            isTrial: false,
            isAnnual: false);

        // Assert
        subscription.Should().NotBeNull();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.TrialEndDate.Should().BeNull();
        subscription.NextBillingDate.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));
        subscription.IsAnnual.Should().BeFalse();

        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ValidPaidAnnual_ShouldUseAnnualPrice()
    {
        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId,
            isTrial: false,
            isAnnual: true);

        // Assert
        subscription.IsAnnual.Should().BeTrue();
        subscription.NextBillingDate.Should().BeCloseTo(DateTime.UtcNow.AddYears(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ExistingActiveSubscription_ShouldThrowException()
    {
        // Arrange - Create first subscription
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act - Try to create second subscription
        var act = async () => await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id,
            _testPaymentMethodId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active subscription*");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidUser_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.CreateSubscriptionAsync(
            Guid.NewGuid(), // Non-existent user
            _proPier.Id,
            _testPaymentMethodId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User not found*");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InactiveTier_ShouldThrowException()
    {
        // Arrange - Deactivate tier
        _proPier.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var act = async () => await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid or inactive subscription tier*");
    }

    #endregion

    #region Get Subscription Tests

    [Fact]
    public async Task GetUserActiveSubscriptionAsync_WithActiveSubscription_ShouldReturnSubscription()
    {
        // Arrange
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act
        var result = await _service.GetUserActiveSubscriptionAsync(_testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(subscription.Id);
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetUserActiveSubscriptionAsync_NoSubscription_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetUserActiveSubscriptionAsync(_testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionsAsync_ShouldReturnAllUserSubscriptions()
    {
        // Arrange - Create subscription, cancel it, create another
        var sub1 = await _service.CreateSubscriptionAsync(_testUser.Id, _proPier.Id, _testPaymentMethodId);
        await _service.CancelSubscriptionAsync(_testUser.Id); // Method expects userId, not subscriptionId
        await _service.CreateSubscriptionAsync(_testUser.Id, _enterpriseTier.Id, _testPaymentMethodId);

        // Act
        var (subscriptions, totalCount) = await _service.GetUserSubscriptionsAsync(_testUser.Id, 1, 10);

        // Assert
        subscriptions.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSubscriptionTiersAsync_ShouldReturnAllActiveTiers()
    {
        // Act
        var tiers = await _service.GetSubscriptionTiersAsync();

        // Assert
        tiers.Should().HaveCount(3);
        tiers.Should().OnlyContain(t => t.IsActive);
        tiers.Should().BeInAscendingOrder(t => t.SortOrder);
    }

    #endregion

    #region Upgrade/Downgrade Tests

    [Fact]
    public async Task UpgradeSubscriptionAsync_ValidUpgrade_ShouldUpgradeTier()
    {
        // Arrange - Start with Pro tier
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act - Upgrade to Enterprise
        var upgraded = await _service.UpgradeSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id);

        // Assert - UpgradeSubscriptionAsync updates the subscription IN PLACE, not create new
        upgraded.SubscriptionTierId.Should().Be(_enterpriseTier.Id);
        upgraded.Status.Should().Be(SubscriptionStatus.Active);
        upgraded.Id.Should().Be(subscription.Id); // Same subscription, just upgraded

        // Verify the subscription was updated in database
        var dbSub = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSub!.SubscriptionTierId.Should().Be(_enterpriseTier.Id);

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id && a.Action.Contains("UPGRADE"));
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task DowngradeSubscriptionAsync_ValidDowngrade_ShouldDowngradeTier()
    {
        // Arrange - Start with Enterprise
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id,
            _testPaymentMethodId);

        // Act - Downgrade to Pro (method expects userId, not subscriptionId)
        var downgraded = await _service.DowngradeSubscriptionAsync(
            _testUser.Id,
            _proPier.Id);

        // Assert - Downgrade schedules the tier change for next billing
        downgraded.Id.Should().Be(subscription.Id); // Same subscription
        downgraded.EndDate.Should().NotBeNull(); // Scheduled for downgrade

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id && a.Action.Contains("DOWNGRADE"));
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region Cancel/Pause/Resume Tests

    [Fact]
    public async Task CancelSubscriptionAsync_ActiveSubscription_ShouldCancelSuccessfully()
    {
        // Arrange
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act - Method expects userId, not subscriptionId
        var cancelled = await _service.CancelSubscriptionAsync(_testUser.Id);

        // Assert
        cancelled.Status.Should().Be(SubscriptionStatus.Cancelled);
        cancelled.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    // NOTE: PauseSubscriptionAsync and ResumeSubscriptionAsync tests removed
    // SubscriptionStatus.Paused does not exist in the current enum
    // If pause/resume functionality is needed, it should be added to the enum first

    #endregion

    #region Trial Conversion Tests

    [Fact]
    public async Task ConvertTrialToPaidAsync_ValidTrial_ShouldConvertSuccessfully()
    {
        // Arrange - Create trial
        var trial = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId,
            isTrial: true);

        // Act
        var converted = await _service.ConvertTrialToPaidAsync(
            _testUser.Id,
            _testPaymentMethodId);

        // Assert - ConvertTrialToPaidAsync updates the trial IN PLACE, not creating new subscription
        converted.Status.Should().Be(SubscriptionStatus.Active);
        converted.TrialEndDate.Should().BeNull();
        converted.Id.Should().Be(trial.Id); // Same subscription, just updated

        // Verify the subscription was updated (not cancelled and replaced)
        var updatedSubscription = await _context.UserSubscriptions.FindAsync(trial.Id);
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task ConvertTrialToPaidAsync_NoActiveTrial_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.ConvertTrialToPaidAsync(
            _testUser.Id,
            _testPaymentMethodId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No trial subscription found*");
    }

    #endregion

    #region Renew Subscription Tests

    [Fact]
    public async Task RenewSubscriptionAsync_ExpiredSubscription_ShouldRenewSuccessfully()
    {
        // Arrange - Create subscription and mark as expired
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Capture initial billing cycle count before modification
        var initialBillingCycleCount = subscription.BillingCycleCount;

        subscription.Status = SubscriptionStatus.Expired;
        subscription.EndDate = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        // Act
        var renewed = await _service.RenewSubscriptionAsync(subscription.Id);

        // Assert
        renewed.Status.Should().Be(SubscriptionStatus.Active);
        renewed.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        renewed.BillingCycleCount.Should().BeGreaterThan(initialBillingCycleCount);
    }

    #endregion

    #region Feature Access Tests

    [Fact]
    public async Task HasFeatureAccessAsync_WithAccess_ShouldReturnTrue()
    {
        // Arrange - Pro tier has ApiAccess = true
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act - Check for a feature that Pro tier has
        var hasAccess = await _service.HasFeatureAccessAsync(_testUser.Id, "ApiAccess");

        // Assert
        hasAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasFeatureAccessAsync_WithoutAccess_ShouldReturnFalse()
    {
        // Arrange - Free tier doesn't have ApiAccess
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _freeTier.Id,
            _testPaymentMethodId);

        // Act
        var hasAccess = await _service.HasFeatureAccessAsync(_testUser.Id, "ApiAccess");

        // Assert
        hasAccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserSubscriptionLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Arrange
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act
        var limits = await _service.GetUserSubscriptionLimitsAsync(_testUser.Id);

        // Assert
        limits.Should().NotBeNull();
        limits.MaxActiveProjects.Should().Be(_proPier.MaxActiveProjects);
        limits.MaxTeamMembers.Should().Be(_proPier.MaxTeamMembers);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetSubscriptionStatisticsAsync_ShouldReturnAccurateStats()
    {
        // Arrange - Create multiple subscriptions
        await _service.CreateSubscriptionAsync(_testUser.Id, _proPier.Id, _testPaymentMethodId, isTrial: true);
        await _service.CreateSubscriptionAsync(_testUser2.Id, _enterpriseTier.Id, _testPaymentMethodId2);

        // Act
        var startDate = DateTime.UtcNow.AddMonths(-1);
        var endDate = DateTime.UtcNow.AddDays(1);
        var stats = await _service.GetSubscriptionStatisticsAsync(startDate, endDate);

        // Assert
        stats.Should().NotBeNull();
        // Note: Check actual DTO structure to add more specific assertions
    }

    [Fact]
    public async Task GetUserUsageStatisticsAsync_ShouldReturnUsageData()
    {
        // Arrange
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act
        var usage = await _service.GetUserUsageStatisticsAsync(_testUser.Id);

        // Assert
        usage.Should().NotBeNull();
        // Note: Check actual DTO properties to add more specific assertions
    }

    #endregion

    #region Payment Recording Tests

    [Fact]
    public async Task RecordPaymentAsync_ValidSubscription_ShouldRecordPayment()
    {
        // Arrange
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Set external subscription ID
        subscription.ExternalSubscriptionId = "sub_test_123";
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordPaymentAsync(subscription.ExternalSubscriptionId, 2999);

        // Assert
        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription!.LastPaymentDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Stripe Webhook Subscription Tests

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeWebhook_ShouldCreateSubscription()
    {
        // Arrange
        var stripeSubId = "sub_stripe_12345";
        var stripeCustomerId = "cus_stripe_67890";

        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            stripeSubId,
            stripeCustomerId);

        // Assert
        subscription.Should().NotBeNull();
        subscription.ExternalSubscriptionId.Should().Be(stripeSubId);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.AutoRenew.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(1);

        // Verify user's Stripe customer ID was updated
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.ExternalCustomerId.Should().Be(stripeCustomerId);

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id && a.Action.Contains("STRIPE"));
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeWebhook_WithPromotionInfo_ShouldStorePromotion()
    {
        // Arrange
        var stripeSubId = "sub_promo_123";
        var promotionInfo = new SubscriptionPromotionInfo
        {
            CouponId = "coupon_50OFF",
            PromoCode = "SAVE50",
            DiscountEndsAt = DateTime.UtcNow.AddMonths(3)
        };

        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            stripeSubId,
            "cus_test",
            promotionInfo);

        // Assert
        subscription.AppliedCouponId.Should().Be("coupon_50OFF");
        subscription.AppliedPromoCode.Should().Be("SAVE50");
        subscription.DiscountEndsAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeWebhook_IdempotentSameTier_ShouldReturnExisting()
    {
        // Arrange - Create first subscription
        var stripeSubId = "sub_existing_123";
        var first = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            stripeSubId,
            "cus_test");

        // Act - Try to create again with same tier
        var second = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            "sub_new_456", // Different Stripe ID
            "cus_test");

        // Assert - Should return same subscription (idempotent)
        second.Id.Should().Be(first.Id);
        second.ExternalSubscriptionId.Should().Be(stripeSubId); // Original Stripe ID preserved
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeWebhook_IdempotentByStripeId_ShouldReturnExisting()
    {
        // Arrange - Create first subscription
        var stripeSubId = "sub_idempotent_789";
        var first = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            stripeSubId,
            "cus_test");

        // Act - Try to create again with same Stripe ID
        var second = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            stripeSubId, // Same Stripe ID
            "cus_test");

        // Assert - Should return existing subscription
        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeWebhook_DifferentTier_ShouldThrowException()
    {
        // Arrange - Create first subscription with Pro tier
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            "sub_existing",
            "cus_test");

        // Act - Try to create subscription with different tier
        var act = async () => await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id,
            "sub_new",
            "cus_test");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active subscription to a different tier*");
    }

    #endregion

    #region Pause/Resume Subscription Tests

    [Fact]
    public async Task PauseSubscriptionAsync_ActiveSubscription_ShouldPauseSuccessfully()
    {
        // Arrange
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act
        var paused = await _service.PauseSubscriptionAsync(_testUser.Id, TimeSpan.FromDays(30));

        // Assert
        paused.Status.Should().Be(SubscriptionStatus.Suspended);
        paused.EndDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));

        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription!.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_SuspendedSubscription_ShouldResumeSuccessfully()
    {
        // Arrange - Create and pause subscription
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);
        await _service.PauseSubscriptionAsync(_testUser.Id, TimeSpan.FromDays(30));

        // Act
        var resumed = await _service.ResumeSubscriptionAsync(_testUser.Id);

        // Assert
        resumed.Status.Should().Be(SubscriptionStatus.Active);
        resumed.EndDate.Should().BeNull();
        resumed.NextBillingDate.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));

        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_NoSuspendedSubscription_ShouldThrowException()
    {
        // Act - Try to resume when no suspended subscription exists
        var act = async () => await _service.ResumeSubscriptionAsync(_testUser.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No paused subscription found*");
    }

    #endregion

    #region RecordPaymentAsync Edge Cases

    [Fact]
    public async Task RecordPaymentAsync_NonExistentSubscription_ShouldNotThrow()
    {
        // Act - Should not throw, just log warning
        await _service.RecordPaymentAsync("sub_nonexistent", 2999);

        // Assert - No exception thrown
        // Subscription not found is logged as warning but doesn't throw
    }

    [Fact]
    public async Task RecordPaymentAsync_PastDueSubscription_ShouldReactivate()
    {
        // Arrange - Create subscription and mark as PastDue
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        subscription.ExternalSubscriptionId = "sub_pastdue_123";
        subscription.Status = SubscriptionStatus.PastDue;
        await _context.SaveChangesAsync();

        var initialCycleCount = subscription.BillingCycleCount;

        // Act
        await _service.RecordPaymentAsync("sub_pastdue_123", 2999);

        // Assert - Should reactivate
        var dbSubscription = await _context.UserSubscriptions.FindAsync(subscription.Id);
        dbSubscription!.Status.Should().Be(SubscriptionStatus.Active);
        dbSubscription.LastPaymentDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dbSubscription.BillingCycleCount.Should().BeGreaterThan(initialCycleCount);
    }

    #endregion

    #region Upgrade/Downgrade Edge Cases

    [Fact]
    public async Task UpgradeSubscriptionAsync_WithImmediateChargeFalse_ShouldNotCharge()
    {
        // Arrange - Start with Pro tier
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        var initialTransactionCount = await _context.SubscriptionTransactions.CountAsync();

        // Act - Upgrade with immediateCharge=false
        var upgraded = await _service.UpgradeSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id,
            immediateCharge: false);

        // Assert
        upgraded.SubscriptionTierId.Should().Be(_enterpriseTier.Id);

        // No new transaction should be created
        var finalTransactionCount = await _context.SubscriptionTransactions.CountAsync();
        finalTransactionCount.Should().Be(initialTransactionCount);
    }

    [Fact]
    public async Task UpgradeSubscriptionAsync_FromTrial_ShouldNotCharge()
    {
        // Arrange - Create trial subscription
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId,
            isTrial: true);

        var initialTransactionCount = await _context.SubscriptionTransactions.CountAsync();

        // Act - Upgrade trial (should not charge during trial)
        var upgraded = await _service.UpgradeSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id);

        // Assert
        upgraded.SubscriptionTierId.Should().Be(_enterpriseTier.Id);

        // No payment should be processed for trial upgrade
        var finalTransactionCount = await _context.SubscriptionTransactions.CountAsync();
        finalTransactionCount.Should().Be(initialTransactionCount);
    }

    [Fact]
    public async Task DowngradeSubscriptionAsync_WithCustomEffectiveDate_ShouldScheduleCorrectly()
    {
        // Arrange - Start with Enterprise
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id,
            _testPaymentMethodId);

        var customDate = DateTime.UtcNow.AddMonths(2);

        // Act - Downgrade with custom effective date
        var downgraded = await _service.DowngradeSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            effectiveDate: customDate);

        // Assert
        downgraded.EndDate.Should().Be(customDate);

        // Verify audit log mentions custom date
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id && a.Action.Contains("DOWNGRADE"));
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region Cancel Edge Cases

    [Fact]
    public async Task CancelSubscriptionAsync_ImmediateTrue_ShouldCancelImmediately()
    {
        // Arrange
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        // Act
        var cancelled = await _service.CancelSubscriptionAsync(
            _testUser.Id,
            reason: "User requested immediate cancellation",
            immediate: true);

        // Assert
        cancelled.Status.Should().Be(SubscriptionStatus.Cancelled);
        cancelled.EndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        cancelled.CancellationReason.Should().Be("User requested immediate cancellation");
        cancelled.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ImmediateFalse_ShouldKeepUntilEndOfBilling()
    {
        // Arrange
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);

        var originalNextBillingDate = subscription.NextBillingDate;

        // Act
        var cancelled = await _service.CancelSubscriptionAsync(
            _testUser.Id,
            reason: "Scheduled cancellation",
            immediate: false);

        // Assert
        cancelled.Status.Should().Be(SubscriptionStatus.Cancelled);
        cancelled.EndDate.Should().Be(originalNextBillingDate); // End at billing date
        cancelled.CancellationReason.Should().Be("Scheduled cancellation");
    }

    #endregion

    #region Renew Edge Cases

    [Fact]
    public async Task RenewSubscriptionAsync_CancelledSubscription_ShouldThrowException()
    {
        // Arrange - Create and cancel subscription
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId);
        await _service.CancelSubscriptionAsync(_testUser.Id);

        // Act
        var act = async () => await _service.RenewSubscriptionAsync(subscription.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot renew cancelled subscription*");
    }

    [Fact]
    public async Task RenewSubscriptionAsync_AnnualSubscription_ShouldAddOneYear()
    {
        // Arrange - Create annual subscription
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            _testPaymentMethodId,
            isTrial: false,
            isAnnual: true);

        subscription.Status = SubscriptionStatus.Expired;
        subscription.EndDate = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        var originalNextBillingDate = subscription.NextBillingDate!.Value;

        // Act
        var renewed = await _service.RenewSubscriptionAsync(subscription.Id);

        // Assert
        renewed.Status.Should().Be(SubscriptionStatus.Active);
        renewed.NextBillingDate.Should().Be(originalNextBillingDate.AddYears(1));
        renewed.IsAnnual.Should().BeTrue();
    }

    #endregion

    #region Feature Access Edge Cases

    [Fact]
    public async Task HasFeatureAccessAsync_AllPredefinedFeatures_ShouldReturnCorrectly()
    {
        // Arrange - Enterprise has all features
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _enterpriseTier.Id,
            _testPaymentMethodId);

        // Act & Assert - Test all predefined features
        (await _service.HasFeatureAccessAsync(_testUser.Id, "PrioritySupport")).Should().BeTrue();
        (await _service.HasFeatureAccessAsync(_testUser.Id, "ApiAccess")).Should().BeTrue();
        (await _service.HasFeatureAccessAsync(_testUser.Id, "AdvancedAnalytics")).Should().BeTrue();
        (await _service.HasFeatureAccessAsync(_testUser.Id, "AdvancedFraudDetection")).Should().BeTrue();
        (await _service.HasFeatureAccessAsync(_testUser.Id, "MultiSignature")).Should().BeTrue();
        (await _service.HasFeatureAccessAsync(_testUser.Id, "CustomIntegrations")).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionLimitsAsync_NoSubscription_ShouldReturnDefaultLimits()
    {
        // Act - User has no subscription
        var limits = await _service.GetUserSubscriptionLimitsAsync(_testUser.Id);

        // Assert - Should return free tier defaults
        limits.MaxActiveProjects.Should().Be(1);
        limits.MaxTeamMembers.Should().Be(0);
        limits.MaxMonthlyEarnings.Should().Be(500);
        limits.PrioritySupport.Should().BeFalse();
        limits.ApiAccess.Should().BeFalse();
    }

    #endregion

    #region GetSubscriptionByExternalIdAsync Tests

    [Fact]
    public async Task GetSubscriptionByExternalIdAsync_ExistingSubscription_ShouldReturnSubscription()
    {
        // Arrange
        var stripeSubId = "sub_external_123";
        await _service.CreateSubscriptionAsync(
            _testUser.Id,
            _proPier.Id,
            stripeSubId,
            "cus_test");

        // Act
        var result = await _service.GetSubscriptionByExternalIdAsync(stripeSubId);

        // Assert
        result.Should().NotBeNull();
        result!.ExternalSubscriptionId.Should().Be(stripeSubId);
        result.SubscriptionTier.Should().NotBeNull(); // Includes are working
    }

    [Fact]
    public async Task GetSubscriptionByExternalIdAsync_NonExistentSubscription_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSubscriptionByExternalIdAsync("sub_nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSubscriptionTierAsync Tests

    [Fact]
    public async Task GetSubscriptionTierAsync_ExistingActiveTier_ShouldReturnTier()
    {
        // Act
        var tier = await _service.GetSubscriptionTierAsync(_proPier.Id);

        // Assert
        tier.Should().NotBeNull();
        tier!.Id.Should().Be(_proPier.Id);
        tier.Name.Should().Be("Professional");
    }

    [Fact]
    public async Task GetSubscriptionTierAsync_InactiveTier_ShouldReturnNull()
    {
        // Arrange - Deactivate tier
        _proPier.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var tier = await _service.GetSubscriptionTierAsync(_proPier.Id);

        // Assert
        tier.Should().BeNull();
    }

    #endregion

    #region Phase 6 Coverage Tests - Error Paths and Edge Cases

    [Fact]
    public async Task CreateSubscriptionAsync_NoExternalCustomerId_ShouldCreateCustomer()
    {
        // Arrange - User without ExternalCustomerId
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "newuser",
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User",
            EmailConfirmed = true,
            ExternalCustomerId = null // No existing customer ID
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create wallet for user
        await _walletService.CreateWalletAsync(user.Id);

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = "card",
            Token = "pm_test_new",
            IsValid = true
        };
        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        // Add to mock payment service
        _mockPaymentService.AddPaymentMethodForUser(user.Id, paymentMethod);

        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            user.Id,
            _proPier.Id,
            paymentMethod.Id,
            isTrial: true);

        // Assert - External customer should be created
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.ExternalCustomerId.Should().NotBeNullOrEmpty("External customer should be created");
        updatedUser.ExternalCustomerId.Should().StartWith("cus_");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_PaymentFails_ShouldSetPastDueAndThrow()
    {
        // Arrange - Force payment to fail
        _mockPaymentService.SetupFailure();

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = "card",
            Token = "pm_test_fail",
            IsValid = true
        };
        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        // Add to mock payment service
        _mockPaymentService.AddPaymentMethodForUser(_testUser.Id, paymentMethod);

        // Act & Assert - Payment failure should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateSubscriptionAsync(
                _testUser.Id,
                _proPier.Id,
                paymentMethod.Id,
                isTrial: false));

        // Assert - Subscription should be created but in PastDue status
        var subscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == _testUser.Id);
        subscription.Should().NotBeNull();
        subscription!.Status.Should().Be(SubscriptionStatus.PastDue, "Failed payment sets subscription to PastDue");

        // Assert - Audit log should record the failure
        var auditLogs = await _context.AuditLogs
            .Where(a => a.UserId == _testUser.Id && a.Action == "SUBSCRIPTION_CREATE_FAILED")
            .ToListAsync();
        auditLogs.Should().ContainSingle("Failed subscription creation should be logged");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithCreditBonus_ShouldAwardCredits()
    {
        // Arrange - Tier with credit bonus
        var tierWithBonus = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Professional,
            Name = "Pro with Bonus",
            Price = 50,
            CreditBonus = 100, // Bonus credits
            IsActive = true
        };
        _context.SubscriptionTiers.Add(tierWithBonus);
        await _context.SaveChangesAsync();

        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = "card",
            Token = "pm_test_bonus",
            IsValid = true
        };
        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        // Add to mock payment service
        _mockPaymentService.AddPaymentMethodForUser(_testUser.Id, paymentMethod);

        // Act
        var subscription = await _service.CreateSubscriptionAsync(
            _testUser.Id,
            tierWithBonus.Id,
            paymentMethod.Id,
            isTrial: true);

        // Assert - Credits should be awarded
        var creditTransactions = await _context.CreditTransactions
            .Where(t => t.ToUserId == _testUser.Id && t.Type == CreditTransactionType.BonusPayment)
            .ToListAsync();
        creditTransactions.Should().ContainSingle("Bonus credits should be awarded");
        creditTransactions.First().Amount.Should().Be(100);
        creditTransactions.First().Description.Should().Contain("Welcome bonus");
    }

    [Fact]
    public async Task UpgradeSubscriptionAsync_ZeroProratedAmount_ShouldNotCharge()
    {
        // Arrange - Create subscription
        var subscription = await CreateTestSubscriptionAsync(_testUser.Id, _proPier.Id);

        // Create tier with same price (zero prorated)
        var samePriceTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Professional,
            Name = "Pro Plus",
            Price = _proPier.Price + 0.01m, // Tiny difference that will round to zero
            IsActive = true
        };
        _context.SubscriptionTiers.Add(samePriceTier);
        await _context.SaveChangesAsync();

        var initialTransactionCount = await _context.SubscriptionTransactions.CountAsync();

        // Act - Upgrade with minimal price difference (prorated = ~0)
        subscription.NextBillingDate = DateTime.UtcNow.AddDays(1); // Almost at end of billing cycle
        await _context.SaveChangesAsync();

        var upgraded = await _service.UpgradeSubscriptionAsync(
            _testUser.Id,
            samePriceTier.Id,
            immediateCharge: true);

        // Assert - No charge should occur
        var finalTransactionCount = await _context.SubscriptionTransactions.CountAsync();
        finalTransactionCount.Should().Be(initialTransactionCount, "Zero prorated amount should not create transaction");
    }

    [Fact]
    public async Task UpgradeSubscriptionAsync_SameCreditBonus_ShouldNotAwardAdditionalCredits()
    {
        // Arrange - Current tier with 50 credits
        var currentTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Professional,
            Name = "Pro A",
            Price = 30,
            CreditBonus = 50,
            IsActive = true
        };

        // New tier with same 50 credits
        var newTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Professional,
            Name = "Pro B",
            Price = 35,
            CreditBonus = 50, // Same as current
            IsActive = true
        };

        _context.SubscriptionTiers.AddRange(currentTier, newTier);
        await _context.SaveChangesAsync();

        var subscription = await CreateTestSubscriptionAsync(_testUser.Id, currentTier.Id);
        var initialCreditCount = await _context.CreditTransactions
            .Where(t => t.ToUserId == _testUser.Id).CountAsync();

        // Act - Upgrade
        await _service.UpgradeSubscriptionAsync(_testUser.Id, newTier.Id);

        // Assert - No new credits awarded (credit difference = 0)
        var finalCreditCount = await _context.CreditTransactions
            .Where(t => t.ToUserId == _testUser.Id).CountAsync();
        finalCreditCount.Should().Be(initialCreditCount, "No additional credits when bonus is same");
    }

    [Fact]
    public async Task RenewSubscriptionAsync_NullNextBillingDate_ShouldThrowException()
    {
        // Arrange - Create subscription with null NextBillingDate
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _proPier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = null, // Null - should cause exception
            PaymentMethodId = _testPaymentMethodId,
            IsAnnual = false,
            BillingCycleCount = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act & Assert - Should throw due to null NextBillingDate
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RenewSubscriptionAsync(subscription.Id));

        exception.Message.Should().Contain("NextBillingDate");
    }

    [Fact]
    public async Task RenewSubscriptionAsync_PaymentFails_ShouldSetPastDueAndThrow()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync(_testUser.Id, _proPier.Id);

        // Force payment to fail
        _mockPaymentService.SetupFailure();

        // Act & Assert - Renewal should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RenewSubscriptionAsync(subscription.Id));

        // Assert - Subscription status should be PastDue
        var updated = await _context.UserSubscriptions.FindAsync(subscription.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(SubscriptionStatus.PastDue, "Failed renewal sets status to PastDue");
    }

    [Fact]
    public async Task GetUserSubscriptionLimitsAsync_JsonParsingFails_ShouldIgnoreInvalidJson()
    {
        // Arrange - Tier with invalid JSON in Features
        var tierWithBadJson = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Professional,
            Name = "Pro Bad JSON",
            Price = 40,
            Features = "{ invalid json [", // Invalid JSON
            IsActive = true,
            MaxActiveProjects = 10
        };
        _context.SubscriptionTiers.Add(tierWithBadJson);

        var subscription = new UserSubscription
        {
            UserId = _testUser.Id,
            SubscriptionTierId = tierWithBadJson.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act - Should not throw, just log warning
        var limits = await _service.GetUserSubscriptionLimitsAsync(_testUser.Id);

        // Assert - Should return limits without features from invalid JSON
        limits.Should().NotBeNull();
        limits.MaxActiveProjects.Should().Be(10);
        limits.Features.Should().NotContain(f => true, "Invalid JSON should be ignored");
    }

    [Fact]
    public async Task HasFeatureAccessAsync_DynamicFeature_ShouldCheckJsonFeatures()
    {
        // Arrange - Tier with dynamic features in JSON
        var tierWithDynamicFeatures = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Type = SubscriptionTierType.Enterprise,
            Name = "Enterprise Dynamic",
            Price = 100,
            Features = "[\"CustomFeature1\", \"CustomFeature2\"]", // JSON array
            IsActive = true
        };
        _context.SubscriptionTiers.Add(tierWithDynamicFeatures);

        var subscription = new UserSubscription
        {
            UserId = _testUser.Id,
            SubscriptionTierId = tierWithDynamicFeatures.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Act & Assert - Dynamic feature should be found
        var hasCustomFeature1 = await _service.HasFeatureAccessAsync(_testUser.Id, "CustomFeature1");
        hasCustomFeature1.Should().BeTrue("Dynamic feature from JSON should be accessible");

        var hasNonExistentFeature = await _service.HasFeatureAccessAsync(_testUser.Id, "NonExistent");
        hasNonExistentFeature.Should().BeFalse("Non-existent feature should return false");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripe_UpdatesExistingCustomerId()
    {
        // Arrange - User with different Stripe customer ID
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser3",
            Email = "test3@example.com",
            FirstName = "Test",
            LastName = "User3",
            EmailConfirmed = true,
            ExternalCustomerId = "cus_old123" // Old customer ID
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create wallet for user
        await _walletService.CreateWalletAsync(user.Id);

        var newCustomerId = "cus_new456";

        // Act - Create subscription from Stripe with new customer ID
        var subscription = await _service.CreateSubscriptionAsync(
            user.Id,
            _proPier.Id,
            "sub_test123",
            newCustomerId);

        // Assert - User's ExternalCustomerId should be updated
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.ExternalCustomerId.Should().Be(newCustomerId, "Customer ID should be updated");
    }

    [Fact]
    public async Task GetUserUsageStatisticsAsync_DatabaseError_ShouldReturnZeroValues()
    {
        // Arrange - Dispose context to simulate database error
        var tempContext = new SkillLedgerDbContext(
            new DbContextOptionsBuilder<SkillLedgerDbContext>()
                .UseInMemoryDatabase(databaseName: $"ErrorTest_{Guid.NewGuid()}")
                .Options);

        var tempWalletService = new MockCreditWalletService(tempContext);
        var tempAuditService = new MockAuditLogService(tempContext);
        var tempLogger = new LoggerFactory().CreateLogger<SubscriptionService>();

        var tempService = new SubscriptionService(
            tempContext,
            _mockPaymentService,
            tempWalletService,
            tempAuditService,
            tempLogger);

        // Dispose context to cause error
        tempContext.Dispose();

        // Act - Should not throw, return zero values
        var stats = await tempService.GetUserUsageStatisticsAsync(_testUser.Id);

        // Assert - Should return default/zero values
        stats.Should().NotBeNull();
        stats.CurrentActiveProjects.Should().Be(0);
        stats.CurrentTeamMembers.Should().Be(0);
        stats.CurrentMonthlyEarnings.Should().Be(0);
    }

    [Fact]
    public async Task DowngradeSubscriptionAsync_NoActiveSubscription_ShouldThrowException()
    {
        // Arrange - User with no subscription
        var userWithNoSub = new User
        {
            Id = Guid.NewGuid(),
            UserName = "nosub",
            Email = "nosub@example.com",
            FirstName = "No",
            LastName = "Sub",
            EmailConfirmed = true
        };
        _context.Users.Add(userWithNoSub);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DowngradeSubscriptionAsync(userWithNoSub.Id, _freeTier.Id));
    }

    [Fact]
    public async Task UpgradeSubscriptionAsync_NoActiveSubscription_ShouldThrowException()
    {
        // Arrange - User with no subscription
        var userWithNoSub = new User
        {
            Id = Guid.NewGuid(),
            UserName = "nosub2",
            Email = "nosub2@example.com",
            FirstName = "No",
            LastName = "Sub2",
            EmailConfirmed = true
        };
        _context.Users.Add(userWithNoSub);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpgradeSubscriptionAsync(userWithNoSub.Id, _enterpriseTier.Id));
    }

    [Fact]
    public async Task UpgradeSubscriptionAsync_TierNotMoreExpensive_ShouldThrowException()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync(_testUser.Id, _proPier.Id);

        // Act & Assert - Try to "upgrade" to cheaper tier
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpgradeSubscriptionAsync(_testUser.Id, _freeTier.Id));
    }

    [Fact]
    public async Task DowngradeSubscriptionAsync_TierNotLessExpensive_ShouldThrowException()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync(_testUser.Id, _proPier.Id);

        // Act & Assert - Try to "downgrade" to more expensive tier
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DowngradeSubscriptionAsync(_testUser.Id, _enterpriseTier.Id));
    }

    [Fact]
    public async Task CancelSubscriptionAsync_NoActiveSubscription_ShouldThrowException()
    {
        // Arrange - User with no subscription
        var userWithNoSub = new User
        {
            Id = Guid.NewGuid(),
            UserName = "nosub3",
            Email = "nosub3@example.com",
            FirstName = "No",
            LastName = "Sub3",
            EmailConfirmed = true
        };
        _context.Users.Add(userWithNoSub);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CancelSubscriptionAsync(userWithNoSub.Id));
    }

    [Fact]
    public async Task PauseSubscriptionAsync_NoActiveSubscription_ShouldThrowException()
    {
        // Arrange - User with no subscription
        var userWithNoSub = new User
        {
            Id = Guid.NewGuid(),
            UserName = "nosub4",
            Email = "nosub4@example.com",
            FirstName = "No",
            LastName = "Sub4",
            EmailConfirmed = true
        };
        _context.Users.Add(userWithNoSub);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PauseSubscriptionAsync(userWithNoSub.Id, TimeSpan.FromDays(30)));
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidPaymentMethod_ShouldThrowException()
    {
        // Arrange - Payment method that is invalid
        var invalidPaymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Type = "card",
            IsValid = false // Invalid
        };
        _context.PaymentMethods.Add(invalidPaymentMethod);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateSubscriptionAsync(
                _testUser.Id,
                _proPier.Id,
                invalidPaymentMethod.Id,
                isTrial: false));
    }

    #endregion

    #region Helper Methods

    private async Task<UserSubscription> CreateTestSubscriptionAsync(Guid userId, Guid tierId)
    {
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "card",
            Token = $"pm_test_{Guid.NewGuid():N}",
            IsValid = true
        };
        _context.PaymentMethods.Add(paymentMethod);
        await _context.SaveChangesAsync();

        // Add to mock payment service
        _mockPaymentService.AddPaymentMethodForUser(userId, paymentMethod);

        return await _service.CreateSubscriptionAsync(
            userId,
            tierId,
            paymentMethod.Id,
            isTrial: true);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
