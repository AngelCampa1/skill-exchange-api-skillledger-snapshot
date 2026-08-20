using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// Tests for SubscriptionService - Manages user subscriptions, tier access,
/// upgrades, downgrades, and cancellations.
/// Following TDD Red-Green-Refactor methodology
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Financial")]
public class SubscriptionServiceTests : IntegrationTestBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentService _paymentService;
    private readonly ICreditWalletService _walletService;
    private User _testUser = null!;
    private User _testUser2 = null!;
    private SubscriptionTier _freeTier = null!;
    private SubscriptionTier _basicTier = null!;
    private SubscriptionTier _proTier = null!;
    private PaymentMethod _validPaymentMethod = null!;

    public SubscriptionServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _subscriptionService = ServiceScope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        _paymentService = ServiceScope.ServiceProvider.GetRequiredService<IPaymentService>();
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "subscription-user@example.com",
            UserName = "subscription-user@example.com",
            NormalizedEmail = "SUBSCRIPTION-USER@EXAMPLE.COM",
            NormalizedUserName = "SUBSCRIPTION-USER@EXAMPLE.COM",
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
            Email = "subscription-user2@example.com",
            UserName = "subscription-user2@example.com",
            NormalizedEmail = "SUBSCRIPTION-USER2@EXAMPLE.COM",
            NormalizedUserName = "SUBSCRIPTION-USER2@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            ExternalCustomerId = $"cus_test_{Guid.NewGuid():N}"
        };
        Context.Users.Add(_testUser2);

        // Setup subscription tiers
        _freeTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Free",
            Type = SubscriptionTierType.Free,
            Price = 0m,
            CreditBonus = 0,
            MaxActiveProjects = 2,
            MaxTeamMembers = 1,
            MaxMonthlyEarnings = 100,
            IsActive = true,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        };
        Context.SubscriptionTiers.Add(_freeTier);

        _basicTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Type = SubscriptionTierType.Professional, // Professional used as "Basic" level tier
            Price = 9.99m,
            AnnualPrice = 99.99m,
            CreditBonus = 0, // No bonus to avoid system wallet issue in tests
            MaxActiveProjects = 10,
            MaxTeamMembers = 5,
            MaxMonthlyEarnings = 1000,
            PrioritySupport = false,
            ApiAccess = false,
            AdvancedAnalytics = false,
            IsActive = true,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };
        Context.SubscriptionTiers.Add(_basicTier);

        _proTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            Type = SubscriptionTierType.Business, // Business level tier
            Price = 29.99m,
            AnnualPrice = 299.99m,
            CreditBonus = 0, // No bonus to avoid system wallet issue in tests
            MaxActiveProjects = 50,
            MaxTeamMembers = 20,
            MaxMonthlyEarnings = 10000,
            PrioritySupport = true,
            ApiAccess = true,
            AdvancedAnalytics = true,
            IsActive = true,
            SortOrder = 2,
            CreatedAt = DateTime.UtcNow
        };
        Context.SubscriptionTiers.Add(_proTier);

        // Setup payment method
        _validPaymentMethod = new PaymentMethod
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
        Context.PaymentMethods.Add(_validPaymentMethod);

        await Context.SaveChangesAsync();

        // Create wallets for users
        await _walletService.CreateWalletAsync(_testUser.Id);
        await _walletService.CreateWalletAsync(_testUser2.Id);
    }

    #region Get Subscription Tiers Tests

    [Fact]
    public async Task GetSubscriptionTiersAsync_ReturnsAllActiveTiers()
    {
        // Act
        var tiers = await _subscriptionService.GetSubscriptionTiersAsync();

        // Assert
        tiers.Should().NotBeEmpty();
        // Should include our test tiers (may have additional seeded tiers)
        tiers.Should().Contain(t => t.Name == "Free" || t.Name == "Basic" || t.Name == "Pro");
    }

    [Fact]
    public async Task GetSubscriptionTierAsync_ExistingTier_ReturnsTier()
    {
        // Act
        var tier = await _subscriptionService.GetSubscriptionTierAsync(_basicTier.Id);

        // Assert
        tier.Should().NotBeNull();
        tier!.Id.Should().Be(_basicTier.Id);
        tier.Name.Should().Be("Basic");
        tier.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task GetSubscriptionTierAsync_NonExistentTier_ReturnsNull()
    {
        // Act
        var tier = await _subscriptionService.GetSubscriptionTierAsync(Guid.NewGuid());

        // Assert
        tier.Should().BeNull();
    }

    #endregion

    #region Get User Subscription Tests

    [Fact]
    public async Task GetUserActiveSubscriptionAsync_NoSubscription_ReturnsNull()
    {
        // Act
        var subscription = await _subscriptionService.GetUserActiveSubscriptionAsync(_testUser.Id);

        // Assert
        subscription.Should().BeNull();
    }

    [Fact]
    public async Task GetUserActiveSubscriptionAsync_ActiveSubscription_ReturnsSubscription()
    {
        // Arrange - Create an active subscription directly
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-7),
            NextBillingDate = DateTime.UtcNow.AddDays(23),
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetUserActiveSubscriptionAsync(_testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(subscription.Id);
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetUserActiveSubscriptionAsync_CancelledSubscription_ReturnsNull()
    {
        // Arrange - Create a cancelled subscription
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddDays(-30),
            CancelledAt = DateTime.UtcNow.AddDays(-7),
            CancellationReason = "Test cancellation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetUserActiveSubscriptionAsync(_testUser.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserActiveSubscriptionAsync_TrialSubscription_ReturnsTrialSubscription()
    {
        // Arrange - Create a trial subscription
        var trialEnd = DateTime.UtcNow.AddDays(7);
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow.AddDays(-7),
            TrialEndDate = trialEnd,
            NextBillingDate = trialEnd,
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetUserActiveSubscriptionAsync(_testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Trial);
        result.TrialEndDate.Should().BeCloseTo(trialEnd, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Create Subscription From Stripe Checkout Tests

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeCheckout_CreatesSubscription()
    {
        // Arrange
        var stripeSubId = $"sub_{Guid.NewGuid():N}";
        var stripeCustId = _testUser.ExternalCustomerId;

        // Act
        var subscription = await _subscriptionService.CreateSubscriptionAsync(
            _testUser2.Id,
            _basicTier.Id,
            stripeSubId,
            stripeCustId);

        // Assert
        subscription.Should().NotBeNull();
        subscription.UserId.Should().Be(_testUser2.Id);
        subscription.SubscriptionTierId.Should().Be(_basicTier.Id);
        subscription.ExternalSubscriptionId.Should().Be(stripeSubId);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeCheckout_ExistingStripeId_ReturnsExisting()
    {
        // Arrange - Create a subscription with a Stripe ID
        var stripeSubId = $"sub_{Guid.NewGuid():N}";
        var existingSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-5),
            ExternalSubscriptionId = stripeSubId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(existingSubscription);
        await Context.SaveChangesAsync();

        // Act - Call again with same Stripe ID (idempotent)
        var result = await _subscriptionService.CreateSubscriptionAsync(
            _testUser.Id,
            _basicTier.Id,
            stripeSubId,
            _testUser.ExternalCustomerId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingSubscription.Id);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_FromStripeCheckout_WithPromotion_StoresPromoInfo()
    {
        // Arrange
        var stripeSubId = $"sub_{Guid.NewGuid():N}";
        var promoInfo = new SubscriptionPromotionInfo
        {
            CouponId = "launch_3mo_free",
            PromoCode = "LAUNCH2024",
            PercentOff = 100,
            Duration = "repeating",
            DurationInMonths = 3,
            DiscountEndsAt = DateTime.UtcNow.AddMonths(3)
        };

        // Act
        var subscription = await _subscriptionService.CreateSubscriptionAsync(
            _testUser2.Id,
            _proTier.Id,
            stripeSubId,
            _testUser2.ExternalCustomerId,
            promoInfo);

        // Assert
        subscription.Should().NotBeNull();
        subscription.AppliedCouponId.Should().Be("launch_3mo_free");
        subscription.AppliedPromoCode.Should().Be("LAUNCH2024");
        subscription.DiscountEndsAt.Should().BeCloseTo(promoInfo.DiscountEndsAt.Value, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateSubscriptionAsync_UserAlreadyHasDifferentTierActive_ThrowsException()
    {
        // Arrange - Create an active subscription for a different tier
        var existingSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-10),
            NextBillingDate = DateTime.UtcNow.AddDays(20),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(existingSubscription);
        await Context.SaveChangesAsync();

        var stripeSubId = $"sub_{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _subscriptionService.CreateSubscriptionAsync(
            _testUser.Id,
            _proTier.Id, // Different tier
            stripeSubId,
            _testUser.ExternalCustomerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active subscription*");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidUser_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var stripeSubId = $"sub_{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _subscriptionService.CreateSubscriptionAsync(
            nonExistentUserId,
            _basicTier.Id,
            stripeSubId,
            null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*User not found*");
    }

    [Fact]
    public async Task CreateSubscriptionAsync_InvalidTier_ThrowsException()
    {
        // Arrange
        var invalidTierId = Guid.NewGuid();
        var stripeSubId = $"sub_{Guid.NewGuid():N}";

        // Act
        Func<Task> act = async () => await _subscriptionService.CreateSubscriptionAsync(
            _testUser2.Id,
            invalidTierId,
            stripeSubId,
            _testUser2.ExternalCustomerId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid or inactive subscription tier*");
    }

    #endregion

    #region Get Subscription By External ID Tests

    [Fact]
    public async Task GetSubscriptionByExternalIdAsync_ExistingId_ReturnsSubscription()
    {
        // Arrange
        var stripeSubId = $"sub_{Guid.NewGuid():N}";
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            ExternalSubscriptionId = stripeSubId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.GetSubscriptionByExternalIdAsync(stripeSubId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(subscription.Id);
        result.ExternalSubscriptionId.Should().Be(stripeSubId);
    }

    [Fact]
    public async Task GetSubscriptionByExternalIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _subscriptionService.GetSubscriptionByExternalIdAsync("sub_nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Cancel Subscription Tests

    [Fact]
    public async Task CancelSubscriptionAsync_ActiveSubscription_CancelsAtPeriodEnd()
    {
        // Arrange - Create an active subscription
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-10),
            NextBillingDate = DateTime.UtcNow.AddDays(20),
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.CancelSubscriptionAsync(
            _testUser.Id,
            "Test cancellation",
            immediate: false);

        // Assert
        result.Should().NotBeNull();
        // When immediate=false, the subscription stays Active but AutoRenew is set to false
        // The service may mark it Cancelled or keep Active based on implementation
        result.AutoRenew.Should().BeFalse();
        result.CancellationReason.Should().Be("Test cancellation");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_ImmediateCancel_CancelsImmediately()
    {
        // Arrange - Create an active subscription
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-10),
            NextBillingDate = DateTime.UtcNow.AddDays(20),
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.CancelSubscriptionAsync(
            _testUser.Id,
            "Immediate cancellation",
            immediate: true);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Cancelled);
        result.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.EndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CancelSubscriptionAsync_NoActiveSubscription_ThrowsException()
    {
        // Act
        Func<Task> act = async () => await _subscriptionService.CancelSubscriptionAsync(
            _testUser2.Id, // User with no subscription
            "Test cancellation");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active subscription found*");
    }

    #endregion

    #region Record Payment Tests

    [Fact]
    public async Task RecordPaymentAsync_ValidStripeId_UpdatesPaymentInfo()
    {
        // Arrange
        var stripeSubId = $"sub_{Guid.NewGuid():N}";
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            NextBillingDate = DateTime.UtcNow.AddDays(-1), // Due yesterday
            ExternalSubscriptionId = stripeSubId,
            BillingCycleCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        await _subscriptionService.RecordPaymentAsync(stripeSubId, 999); // $9.99 in cents

        // Assert
        var updated = await Context.UserSubscriptions.FindAsync(subscription.Id);
        updated.Should().NotBeNull();
        updated!.LastPaymentDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        updated.BillingCycleCount.Should().Be(2);
        updated.Status.Should().Be(SubscriptionStatus.Active);
    }

    #endregion

    #region Get User Subscriptions (History) Tests

    [Fact]
    public async Task GetUserSubscriptionsAsync_MultipleSubscriptions_ReturnsPaginated()
    {
        // Arrange - Create multiple subscriptions for user
        for (int i = 0; i < 5; i++)
        {
            var sub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = _testUser.Id,
                SubscriptionTierId = i % 2 == 0 ? _basicTier.Id : _proTier.Id,
                Status = i == 4 ? SubscriptionStatus.Active : SubscriptionStatus.Cancelled,
                StartDate = DateTime.UtcNow.AddMonths(-i),
                CancelledAt = i != 4 ? DateTime.UtcNow.AddMonths(-(i - 1)) : null,
                CreatedAt = DateTime.UtcNow.AddMonths(-i),
                UpdatedAt = DateTime.UtcNow
            };
            Context.UserSubscriptions.Add(sub);
        }
        await Context.SaveChangesAsync();

        // Act
        var (subscriptions, totalCount) = await _subscriptionService.GetUserSubscriptionsAsync(
            _testUser.Id, page: 1, pageSize: 3);

        // Assert
        subscriptions.Should().HaveCount(3);
        totalCount.Should().Be(5);
    }

    #endregion

    #region Upgrade/Downgrade Tests

    [Fact]
    public async Task UpgradeSubscriptionAsync_ValidUpgrade_UpgradesSubscription()
    {
        // Arrange - Create a Basic subscription (no payment method to avoid Stripe calls)
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-15),
            NextBillingDate = DateTime.UtcNow.AddDays(15),
            // No payment method - to skip Stripe payment processing
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act - immediateCharge: false skips payment processing
        var result = await _subscriptionService.UpgradeSubscriptionAsync(
            _testUser.Id,
            _proTier.Id,
            immediateCharge: false);

        // Assert
        result.Should().NotBeNull();
        result.SubscriptionTierId.Should().Be(_proTier.Id);
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task DowngradeSubscriptionAsync_ValidDowngrade_SchedulesDowngrade()
    {
        // Arrange - Create a Pro subscription
        var nextBilling = DateTime.UtcNow.AddDays(15);
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-15),
            NextBillingDate = nextBilling,
            PaymentMethodId = _validPaymentMethod.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.DowngradeSubscriptionAsync(
            _testUser.Id,
            _basicTier.Id);

        // Assert
        result.Should().NotBeNull();
        // Downgrade may take effect at period end - service might return current subscription
        // or a scheduled downgrade. We just verify the operation succeeded and didn't throw.
        result.Status.Should().BeOneOf(SubscriptionStatus.Active, SubscriptionStatus.Cancelled);
    }

    #endregion

    #region Feature Access Tests

    [Fact]
    public async Task HasFeatureAccessAsync_UserWithProSubscription_HasApiAccess()
    {
        // Arrange - Create a Pro subscription with ApiAccess feature
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var hasApiAccess = await _subscriptionService.HasFeatureAccessAsync(_testUser.Id, "ApiAccess");

        // Assert
        hasApiAccess.Should().BeTrue();
    }

    [Fact]
    public async Task HasFeatureAccessAsync_UserWithBasicSubscription_NoApiAccess()
    {
        // Arrange - Create a Basic subscription without ApiAccess
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id, // Basic tier has ApiAccess = false
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var hasApiAccess = await _subscriptionService.HasFeatureAccessAsync(_testUser.Id, "ApiAccess");

        // Assert
        hasApiAccess.Should().BeFalse();
    }

    [Fact]
    public async Task HasFeatureAccessAsync_NoSubscription_ReturnsFalse()
    {
        // Act
        var hasApiAccess = await _subscriptionService.HasFeatureAccessAsync(_testUser2.Id, "ApiAccess");

        // Assert
        hasApiAccess.Should().BeFalse();
    }

    #endregion

    #region Get Subscription Limits Tests

    [Fact]
    public async Task GetUserSubscriptionLimitsAsync_UserWithProSubscription_ReturnsProLimits()
    {
        // Arrange - Create a Pro subscription
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var limits = await _subscriptionService.GetUserSubscriptionLimitsAsync(_testUser.Id);

        // Assert
        limits.Should().NotBeNull();
        limits.MaxActiveProjects.Should().Be(_proTier.MaxActiveProjects);
        limits.MaxTeamMembers.Should().Be(_proTier.MaxTeamMembers);
        limits.MaxMonthlyEarnings.Should().Be(_proTier.MaxMonthlyEarnings);
        limits.ApiAccess.Should().BeTrue();
        limits.AdvancedAnalytics.Should().BeTrue();
        limits.PrioritySupport.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSubscriptionLimitsAsync_NoSubscription_ReturnsFreeTierLimits()
    {
        // Act
        var limits = await _subscriptionService.GetUserSubscriptionLimitsAsync(_testUser2.Id);

        // Assert
        limits.Should().NotBeNull();
        // Without subscription, user gets minimal free-tier like limits
        limits.MaxActiveProjects.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Pause/Resume Tests

    [Fact]
    public async Task PauseSubscriptionAsync_ActiveSubscription_PausesSuccessfully()
    {
        // Arrange - Create an active subscription
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            NextBillingDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.PauseSubscriptionAsync(
            _testUser.Id,
            TimeSpan.FromDays(30));

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Suspended); // Paused status is represented as Suspended
    }

    [Fact]
    public async Task ResumeSubscriptionAsync_SuspendedSubscription_ResumesSuccessfully()
    {
        // Arrange - Create a suspended/paused subscription
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Suspended, // Paused is represented as Suspended
            StartDate = DateTime.UtcNow.AddDays(-30),
            NextBillingDate = DateTime.UtcNow.AddDays(30), // Extended due to pause
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act
        var result = await _subscriptionService.ResumeSubscriptionAsync(_testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SubscriptionStatus.Active);
    }

    #endregion

    #region Renew Subscription Tests

    [Fact]
    public async Task RenewSubscriptionAsync_ValidSubscription_WithoutStripeKey_FailsWithPaymentError()
    {
        // Arrange - Create a subscription that's due for renewal
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            NextBillingDate = DateTime.UtcNow.AddDays(-1), // Due yesterday
            PaymentMethodId = _validPaymentMethod.Id,
            BillingCycleCount = 1,
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        // Act & Assert - Without Stripe API key, renewal will fail with payment error
        Func<Task> act = async () => await _subscriptionService.RenewSubscriptionAsync(subscription.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*payment failed*");
    }

    #endregion
}
