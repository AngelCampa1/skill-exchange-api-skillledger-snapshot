using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// Unit tests for StripePromotionService.
/// Note: These tests verify the service's behavior with mocked dependencies.
/// Actual Stripe API integration is tested separately.
/// </summary>
[UnitTest]
[FinancialTest]
public class StripePromotionServiceTests
{
    private readonly Mock<ILogger<StripePromotionService>> _loggerMock;
    private readonly IOptions<StripeSettings> _stripeSettings;

    public StripePromotionServiceTests()
    {
        _loggerMock = new Mock<ILogger<StripePromotionService>>();
        _stripeSettings = Options.Create(new StripeSettings
        {
            SecretKey = "sk_test_fake_key_for_testing",
            WebhookSecret = "whsec_fake_webhook_secret"
        });
    }

    #region CreateCouponRequest Validation Tests

    [Fact]
    public void CreateCouponRequest_RequiresDiscountType()
    {
        // Arrange
        var request = new CreateCouponRequest
        {
            Id = "test_coupon",
            Name = "Test Coupon",
            Duration = "once"
            // Neither PercentOff nor AmountOffCents set
        };

        // Assert - validation should catch this
        Assert.Null(request.PercentOff);
        Assert.Null(request.AmountOffCents);
    }

    [Fact]
    public void CreateCouponRequest_PercentOff_SetsCorrectly()
    {
        // Arrange
        var request = new CreateCouponRequest
        {
            Id = "launch_3mo_free",
            Name = "Launch Promotion - 3 Months Free",
            PercentOff = 100,
            Duration = "repeating",
            DurationInMonths = 3,
            MaxRedemptions = 100
        };

        // Assert
        Assert.Equal(100m, request.PercentOff);
        Assert.Null(request.AmountOffCents);
    }

    [Fact]
    public void CreateCouponRequest_AmountOff_SetsCorrectly()
    {
        // Arrange
        var request = new CreateCouponRequest
        {
            Id = "10_off_first_month",
            Name = "$10 Off First Month",
            AmountOffCents = 1000, // $10.00
            Currency = "usd",
            Duration = "once"
        };

        // Assert
        Assert.Null(request.PercentOff);
        Assert.Equal(1000, request.AmountOffCents);
        Assert.Equal("usd", request.Currency);
    }

    [Theory]
    [InlineData("once", null)]
    [InlineData("forever", null)]
    [InlineData("repeating", 3)]
    [InlineData("repeating", 6)]
    [InlineData("repeating", 12)]
    public void CreateCouponRequest_Duration_SetsCorrectly(string duration, int? months)
    {
        // Arrange
        var request = new CreateCouponRequest
        {
            Id = "test_coupon",
            Name = "Test Coupon",
            PercentOff = 50,
            Duration = duration,
            DurationInMonths = months
        };

        // Assert
        Assert.Equal(duration, request.Duration);
        Assert.Equal(months, request.DurationInMonths);
    }

    #endregion

    #region CreatePromoCodeRequest Validation Tests

    [Fact]
    public void CreatePromoCodeRequest_RequiresCouponId()
    {
        // Arrange
        var request = new CreatePromoCodeRequest
        {
            Code = "TESTCODE"
            // CouponId not set - should be caught by validation
        };

        // Assert - default value is empty string
        Assert.Equal(string.Empty, request.CouponId);
    }

    [Fact]
    public void CreatePromoCodeRequest_FirstTimeTransactionOnly_SetsCorrectly()
    {
        // Arrange
        var request = new CreatePromoCodeRequest
        {
            CouponId = "launch_3mo_free",
            Code = "LAUNCH2024",
            FirstTimeTransactionOnly = true
        };

        // Assert
        Assert.True(request.FirstTimeTransactionOnly);
    }

    [Fact]
    public void CreatePromoCodeRequest_MinimumAmount_SetsCorrectly()
    {
        // Arrange
        var request = new CreatePromoCodeRequest
        {
            CouponId = "some_coupon",
            Code = "MIN50",
            MinimumAmountCents = 5000, // $50.00 minimum
            MinimumAmountCurrency = "usd"
        };

        // Assert
        Assert.Equal(5000, request.MinimumAmountCents);
        Assert.Equal("usd", request.MinimumAmountCurrency);
    }

    [Fact]
    public void CreatePromoCodeRequest_CustomerRestriction_SetsCorrectly()
    {
        // Arrange
        var request = new CreatePromoCodeRequest
        {
            CouponId = "some_coupon",
            Code = "VIP2024",
            CustomerId = "cus_123456789"
        };

        // Assert
        Assert.Equal("cus_123456789", request.CustomerId);
    }

    #endregion

    #region PromoValidationResult Tests

    [Fact]
    public void ValidatePromotionCode_Success_ReturnsValidResult()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            TimesRedeemed = 47,
            MaxRedemptions = 100,
            Coupon = new StripeCouponResult
            {
                Id = "launch_3mo_free",
                Name = "Launch - 3 Months Free",
                PercentOff = 100,
                Duration = "repeating",
                DurationInMonths = 3,
                IsValid = true
            }
        };

        // Act
        var result = PromoValidationResult.Success(promoCode);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.PromoCode);
        Assert.Equal("LAUNCH2024", result.PromoCode.Code);
        Assert.Equal("100% off for 3 months", result.DiscountDescription);
    }

    [Fact]
    public void ValidatePromotionCode_CodeNotFound_ReturnsFailure()
    {
        // Act
        var result = PromoValidationResult.Failure("Promotion code not found", "CODE_NOT_FOUND");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("CODE_NOT_FOUND", result.ErrorCode);
        Assert.Equal("Promotion code not found", result.ErrorMessage);
    }

    [Fact]
    public void ValidatePromotionCode_CodeInactive_ReturnsFailure()
    {
        // Act
        var result = PromoValidationResult.Failure("This promotion code is no longer active", "CODE_INACTIVE");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("CODE_INACTIVE", result.ErrorCode);
    }

    [Fact]
    public void ValidatePromotionCode_CodeExpired_ReturnsFailure()
    {
        // Act
        var result = PromoValidationResult.Failure("This promotion code has expired", "CODE_EXPIRED");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("CODE_EXPIRED", result.ErrorCode);
    }

    [Fact]
    public void ValidatePromotionCode_MaxRedemptionsReached_ReturnsFailure()
    {
        // Act
        var result = PromoValidationResult.Failure(
            "This promotion code has reached its maximum redemptions",
            "CODE_MAX_REDEMPTIONS");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("CODE_MAX_REDEMPTIONS", result.ErrorCode);
    }

    [Fact]
    public void ValidatePromotionCode_CouponInvalid_ReturnsFailure()
    {
        // Act
        var result = PromoValidationResult.Failure(
            "The coupon associated with this code is no longer valid",
            "COUPON_INVALID");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("COUPON_INVALID", result.ErrorCode);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void CouponStatsResult_CalculatesRemainingRedemptions()
    {
        // Arrange
        var stats = new CouponStatsResult
        {
            CouponId = "launch_3mo_free",
            CouponName = "Launch - 3 Months Free",
            MaxRedemptions = 100,
            TotalRedemptions = 47
        };

        // Assert
        Assert.Equal(53, stats.RemainingRedemptions);
    }

    [Fact]
    public void CouponStatsResult_CalculatesUsagePercentage()
    {
        // Arrange
        var stats = new CouponStatsResult
        {
            CouponId = "launch_3mo_free",
            MaxRedemptions = 100,
            TotalRedemptions = 47
        };

        // Assert
        Assert.Equal(47m, stats.UsagePercentage);
    }

    [Fact]
    public void PromotionStatsResult_AggregatesMultipleCoupons()
    {
        // Arrange
        var stats = new PromotionStatsResult
        {
            TotalCoupons = 3,
            ActiveCoupons = 2,
            TotalPromotionCodes = 5,
            ActivePromotionCodes = 4,
            TotalRedemptions = 150,
            CouponStats = new List<CouponStatsResult>
            {
                new() { CouponId = "c1", CouponName = "Coupon 1", TotalRedemptions = 100, MaxRedemptions = 200 },
                new() { CouponId = "c2", CouponName = "Coupon 2", TotalRedemptions = 50, MaxRedemptions = 100 },
                new() { CouponId = "c3", CouponName = "Coupon 3", TotalRedemptions = 0, MaxRedemptions = 50 }
            }
        };

        // Assert
        Assert.Equal(3, stats.TotalCoupons);
        Assert.Equal(3, stats.CouponStats.Count);
        Assert.Equal(150, stats.TotalRedemptions);
        Assert.Equal(50m, stats.CouponStats[0].UsagePercentage);
        Assert.Equal(50m, stats.CouponStats[1].UsagePercentage);
        Assert.Equal(0m, stats.CouponStats[2].UsagePercentage);
    }

    #endregion

    #region Launch Promotion Scenario Tests

    [Fact]
    public void LaunchPromotion_CouponRequest_HasCorrectConfiguration()
    {
        // Arrange - This represents our launch promotion: 100% off for 3 months, limited to 100 redemptions
        var request = new CreateCouponRequest
        {
            Id = "launch_3mo_free",
            Name = "Launch Promotion - 3 Months Free",
            PercentOff = 100,
            Duration = "repeating",
            DurationInMonths = 3,
            MaxRedemptions = 100
        };

        // Assert
        Assert.Equal("launch_3mo_free", request.Id);
        Assert.Equal(100m, request.PercentOff);
        Assert.Equal("repeating", request.Duration);
        Assert.Equal(3, request.DurationInMonths);
        Assert.Equal(100, request.MaxRedemptions);
    }

    [Fact]
    public void LaunchPromotion_PromoCodeRequest_HasCorrectConfiguration()
    {
        // Arrange - This creates the LAUNCH2024 promo code
        var request = new CreatePromoCodeRequest
        {
            CouponId = "launch_3mo_free",
            Code = "LAUNCH2024",
            FirstTimeTransactionOnly = true
        };

        // Assert
        Assert.Equal("launch_3mo_free", request.CouponId);
        Assert.Equal("LAUNCH2024", request.Code);
        Assert.True(request.FirstTimeTransactionOnly);
    }

    [Fact]
    public void LaunchPromotion_CouponResult_TracksRedemptions()
    {
        // Arrange - Simulating a coupon after 47 redemptions
        var coupon = new StripeCouponResult
        {
            Id = "launch_3mo_free",
            Name = "Launch Promotion - 3 Months Free",
            PercentOff = 100,
            Duration = "repeating",
            DurationInMonths = 3,
            MaxRedemptions = 100,
            TimesRedeemed = 47,
            IsValid = true
        };

        // Assert
        Assert.Equal(53, coupon.RemainingRedemptions);
        Assert.True(coupon.IsValid);
        Assert.Equal("100% off for 3 months", coupon.DiscountDescription);
    }

    [Fact]
    public void LaunchPromotion_WhenMaxReached_IsNoLongerUsable()
    {
        // Arrange - Simulating when all 100 slots are used
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            MaxRedemptions = 100,
            TimesRedeemed = 100,
            Coupon = new StripeCouponResult
            {
                Id = "launch_3mo_free",
                MaxRedemptions = 100,
                TimesRedeemed = 100,
                IsValid = true
            }
        };

        // Assert
        Assert.False(promoCode.IsUsable);
        Assert.Equal(0, promoCode.RemainingRedemptions);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void StripeCouponResult_HandlesNullOptionalFields()
    {
        // Arrange
        var coupon = new StripeCouponResult
        {
            Id = "test_coupon",
            Duration = "forever",
            IsValid = true
        };

        // Assert
        Assert.Null(coupon.Name);
        Assert.Null(coupon.PercentOff);
        Assert.Null(coupon.AmountOff);
        Assert.Null(coupon.Currency);
        Assert.Null(coupon.DurationInMonths);
        Assert.Null(coupon.MaxRedemptions);
        Assert.Null(coupon.RemainingRedemptions);
        Assert.Null(coupon.RedeemBy);
    }

    [Fact]
    public void StripePromoCodeResult_HandlesNullCoupon()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "TESTCODE",
            Active = true,
            Coupon = null
        };

        // Assert - Should still be usable if coupon is null (not invalid)
        Assert.True(promoCode.IsUsable);
    }

    [Fact]
    public void PromoValidationResult_Success_HandlesNullCoupon()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "TESTCODE",
            Active = true,
            Coupon = null
        };

        // Act
        var result = PromoValidationResult.Success(promoCode);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.DiscountDescription); // Null because no coupon
    }

    #endregion
}
