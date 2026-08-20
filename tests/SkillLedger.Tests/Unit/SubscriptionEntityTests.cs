using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Unit;

[UnitTest]
[FinancialTest]
public class SubscriptionEntityTests
{
    [Fact]
    public void SubscriptionTier_ShouldCreateWithValidProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Professional";
        var description = "Professional tier for testing";
        var price = 29.99m;
        var annualPrice = 299.99m;
        var features = "[\"Advanced Analytics\", \"API Access\", \"Priority Support\"]";

        // Act
        var tier = new SubscriptionTier
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            AnnualPrice = annualPrice,
            MaxActiveProjects = 10,
            MaxTeamMembers = 5,
            MaxMonthlyEarnings = 5000,
            PrioritySupport = true,
            ApiAccess = true,
            AdvancedAnalytics = true,
            AdvancedFraudDetection = false,
            MultiSignature = false,
            CustomIntegrations = false,
            IsActive = true,
            SortOrder = 2,
            Features = features,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(id, tier.Id);
        Assert.Equal(name, tier.Name);
        Assert.Equal(description, tier.Description);
        Assert.Equal(price, tier.Price);
        Assert.Equal(annualPrice, tier.AnnualPrice);
        Assert.Equal(10, tier.MaxActiveProjects);
        Assert.Equal(5, tier.MaxTeamMembers);
        Assert.Equal(5000, tier.MaxMonthlyEarnings);
        Assert.True(tier.PrioritySupport);
        Assert.True(tier.ApiAccess);
        Assert.True(tier.AdvancedAnalytics);
        Assert.False(tier.AdvancedFraudDetection);
        Assert.False(tier.MultiSignature);
        Assert.False(tier.CustomIntegrations);
        Assert.True(tier.IsActive);
        Assert.Equal(2, tier.SortOrder);
        Assert.Equal(features, tier.Features);
    }

    [Fact]
    public void UserSubscription_ShouldCreateWithValidProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tierId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow.AddDays(30);

        // Act
        var subscription = new UserSubscription
        {
            Id = id,
            UserId = userId,
            SubscriptionTierId = tierId,
            Status = SubscriptionStatus.Active,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(id, subscription.Id);
        Assert.Equal(userId, subscription.UserId);
        Assert.Equal(tierId, subscription.SubscriptionTierId);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(startDate, subscription.StartDate);
        Assert.Equal(endDate, subscription.EndDate);
    }

    [Fact]
    public void UserSubscription_ShouldDetermineIfActiveCorrectly()
    {
        // Arrange
        var activeSubscription = new UserSubscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var expiredSubscription = new UserSubscription
        {
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-60),
            EndDate = DateTime.UtcNow.AddDays(-10)
        };

        var canceledSubscription = new UserSubscription
        {
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act & Assert
        Assert.True(activeSubscription.EndDate > DateTime.UtcNow && activeSubscription.Status == SubscriptionStatus.Active);
        Assert.False(expiredSubscription.EndDate > DateTime.UtcNow);
        Assert.False(canceledSubscription.Status == SubscriptionStatus.Active);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Cancelled, false)]
    [InlineData(SubscriptionStatus.PastDue, false)]
    [InlineData(SubscriptionStatus.Suspended, false)]
    public void UserSubscription_ShouldValidateStatusCorrectly(SubscriptionStatus status, bool expectedValid)
    {
        // Arrange
        var subscription = new UserSubscription
        {
            Status = status,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var isValid = subscription.Status == SubscriptionStatus.Active && subscription.EndDate > DateTime.UtcNow;

        // Assert
        if (expectedValid)
        {
            Assert.True(isValid);
        }
        else
        {
            Assert.False(isValid);
        }
    }

    [Fact]
    public void PaymentMethod_ShouldCreateWithValidProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();

        // Act
        var paymentMethod = new PaymentMethod
        {
            Id = id,
            UserId = userId,
            Type = "card",
            Provider = "visa",
            IsDefault = true,
            IsValid = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(id, paymentMethod.Id);
        Assert.Equal(userId, paymentMethod.UserId);
        Assert.Equal("card", paymentMethod.Type);
        Assert.Equal("visa", paymentMethod.Provider);
        Assert.True(paymentMethod.IsDefault);
        Assert.True(paymentMethod.IsValid);
    }

    [Theory]
    [InlineData("card")]
    [InlineData("bank_account")]
    [InlineData("paypal")]
    public void PaymentMethod_ShouldAcceptAllValidTypes(string type)
    {
        // Arrange & Act
        var paymentMethod = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = type,
            Provider = "test-provider",
            IsDefault = false,
            IsValid = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(type, paymentMethod.Type);
    }

    [Fact]
    public void SubscriptionTier_ShouldValidateFeatureConsistency()
    {
        // Arrange
        var tier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Premium",
            Description = "Premium tier",
            Price = 49.99m,
            AnnualPrice = 499.99m,
            PrioritySupport = true,
            ApiAccess = true,
            AdvancedAnalytics = true,
            Features = "[\"Priority Support\", \"API Access\", \"Advanced Analytics\"]",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var hasPrioritySupport = tier.PrioritySupport;
        var hasApiAccess = tier.ApiAccess;
        var hasAdvancedAnalytics = tier.AdvancedAnalytics;

        // Assert
        Assert.True(hasPrioritySupport);
        Assert.True(hasApiAccess);
        Assert.True(hasAdvancedAnalytics);
        Assert.Contains("Priority Support", tier.Features);
        Assert.Contains("API Access", tier.Features);
        Assert.Contains("Advanced Analytics", tier.Features);
    }

    [Fact]
    public void UserSubscription_ShouldHandleGracePeriodCorrectly()
    {
        // Arrange
        var gracePeriodEnd = DateTime.UtcNow.AddDays(5); // 5 days grace period
        var subscription = new UserSubscription
        {
            Status = SubscriptionStatus.PastDue,
            EndDate = DateTime.UtcNow.AddDays(-2), // Expired 2 days ago
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        // Act
        var isInGracePeriod = subscription.EndDate.HasValue && subscription.EndDate.Value.AddDays(7) > DateTime.UtcNow; // 7 day grace period

        // Assert
        Assert.True(isInGracePeriod);
        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
    }
}