using SkillLedger.Core.DTOs;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Unit;

/// <summary>
/// Unit tests for Promotion DTOs and their computed properties.
/// </summary>
[UnitTest]
[FinancialTest]
public class PromotionDtosTests
{
    #region StripeCouponResult Tests

    [Fact]
    public void StripeCouponResult_RemainingRedemptions_CalculatesCorrectly()
    {
        // Arrange
        var coupon = new StripeCouponResult
        {
            Id = "test_coupon",
            MaxRedemptions = 100,
            TimesRedeemed = 47
        };

        // Act
        var remaining = coupon.RemainingRedemptions;

        // Assert
        Assert.Equal(53, remaining);
    }

    [Fact]
    public void StripeCouponResult_RemainingRedemptions_ReturnsNullWhenNoMax()
    {
        // Arrange
        var coupon = new StripeCouponResult
        {
            Id = "test_coupon",
            MaxRedemptions = null,
            TimesRedeemed = 100
        };

        // Act
        var remaining = coupon.RemainingRedemptions;

        // Assert
        Assert.Null(remaining);
    }

    [Theory]
    [InlineData(100, "once", null, "100% off first payment")]
    [InlineData(50, "forever", null, "50% off forever")]
    [InlineData(25, "repeating", 3, "25% off for 3 months")]
    public void StripeCouponResult_DiscountDescription_FormatsPercentageCorrectly(
        decimal percentOff, string duration, int? durationInMonths, string expected)
    {
        // Arrange
        var coupon = new StripeCouponResult
        {
            Id = "test_coupon",
            PercentOff = percentOff,
            Duration = duration,
            DurationInMonths = durationInMonths
        };

        // Act
        var description = coupon.DiscountDescription;

        // Assert
        Assert.Equal(expected, description);
    }

    [Theory]
    [InlineData(1000, "once", null, "$10.00 off first payment")]
    [InlineData(2500, "forever", null, "$25.00 off forever")]
    [InlineData(500, "repeating", 6, "$5.00 off for 6 months")]
    public void StripeCouponResult_DiscountDescription_FormatsAmountCorrectly(
        long amountOffCents, string duration, int? durationInMonths, string expected)
    {
        // Arrange
        var coupon = new StripeCouponResult
        {
            Id = "test_coupon",
            AmountOff = amountOffCents,
            Currency = "usd",
            Duration = duration,
            DurationInMonths = durationInMonths
        };

        // Act
        var description = coupon.DiscountDescription;

        // Assert
        Assert.Equal(expected, description);
    }

    [Fact]
    public void StripeCouponResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var coupon = new StripeCouponResult();

        // Assert
        Assert.Equal(string.Empty, coupon.Id);
        Assert.Equal(string.Empty, coupon.Duration);
        Assert.Equal(0, coupon.TimesRedeemed);
        Assert.False(coupon.IsValid);
        Assert.Empty(coupon.AppliesTo);
        Assert.Empty(coupon.Metadata);
    }

    #endregion

    #region StripePromoCodeResult Tests

    [Fact]
    public void StripePromoCodeResult_RemainingRedemptions_CalculatesCorrectly()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            MaxRedemptions = 100,
            TimesRedeemed = 25
        };

        // Act
        var remaining = promoCode.RemainingRedemptions;

        // Assert
        Assert.Equal(75, remaining);
    }

    [Fact]
    public void StripePromoCodeResult_IsUsable_ReturnsTrueWhenActive()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            MaxRedemptions = 100,
            TimesRedeemed = 25,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Coupon = new StripeCouponResult { IsValid = true }
        };

        // Act
        var isUsable = promoCode.IsUsable;

        // Assert
        Assert.True(isUsable);
    }

    [Fact]
    public void StripePromoCodeResult_IsUsable_ReturnsFalseWhenInactive()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = false
        };

        // Act
        var isUsable = promoCode.IsUsable;

        // Assert
        Assert.False(isUsable);
    }

    [Fact]
    public void StripePromoCodeResult_IsUsable_ReturnsFalseWhenExpired()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };

        // Act
        var isUsable = promoCode.IsUsable;

        // Assert
        Assert.False(isUsable);
    }

    [Fact]
    public void StripePromoCodeResult_IsUsable_ReturnsFalseWhenMaxRedemptionsReached()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            MaxRedemptions = 100,
            TimesRedeemed = 100
        };

        // Act
        var isUsable = promoCode.IsUsable;

        // Assert
        Assert.False(isUsable);
    }

    [Fact]
    public void StripePromoCodeResult_IsUsable_ReturnsFalseWhenCouponInvalid()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            Coupon = new StripeCouponResult { IsValid = false }
        };

        // Act
        var isUsable = promoCode.IsUsable;

        // Assert
        Assert.False(isUsable);
    }

    [Fact]
    public void StripePromoCodeResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var promoCode = new StripePromoCodeResult();

        // Assert
        Assert.Equal(string.Empty, promoCode.Id);
        Assert.Equal(string.Empty, promoCode.Code);
        Assert.False(promoCode.Active);
        Assert.Equal(0, promoCode.TimesRedeemed);
        Assert.False(promoCode.FirstTimeTransactionOnly);
        Assert.Empty(promoCode.Metadata);
    }

    #endregion

    #region PromoValidationResult Tests

    [Fact]
    public void PromoValidationResult_Success_CreatesValidResult()
    {
        // Arrange
        var promoCode = new StripePromoCodeResult
        {
            Id = "promo_123",
            Code = "LAUNCH2024",
            Active = true,
            Coupon = new StripeCouponResult
            {
                Id = "coupon_123",
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
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void PromoValidationResult_Failure_CreatesInvalidResult()
    {
        // Act
        var result = PromoValidationResult.Failure("Code not found", "CODE_NOT_FOUND");

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.PromoCode);
        Assert.Null(result.DiscountDescription);
        Assert.Equal("Code not found", result.ErrorMessage);
        Assert.Equal("CODE_NOT_FOUND", result.ErrorCode);
    }

    [Theory]
    [InlineData("CODE_NOT_FOUND", "Promotion code not found")]
    [InlineData("CODE_INACTIVE", "This promotion code is no longer active")]
    [InlineData("CODE_EXPIRED", "This promotion code has expired")]
    [InlineData("CODE_MAX_REDEMPTIONS", "This promotion code has reached its maximum redemptions")]
    [InlineData("COUPON_INVALID", "The coupon associated with this code is no longer valid")]
    public void PromoValidationResult_Failure_SupportsVariousErrorCodes(string errorCode, string errorMessage)
    {
        // Act
        var result = PromoValidationResult.Failure(errorMessage, errorCode);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    #endregion

    #region CouponStatsResult Tests

    [Fact]
    public void CouponStatsResult_RemainingRedemptions_CalculatesCorrectly()
    {
        // Arrange
        var stats = new CouponStatsResult
        {
            CouponId = "coupon_123",
            MaxRedemptions = 100,
            TotalRedemptions = 75
        };

        // Act
        var remaining = stats.RemainingRedemptions;

        // Assert
        Assert.Equal(25, remaining);
    }

    [Fact]
    public void CouponStatsResult_UsagePercentage_CalculatesCorrectly()
    {
        // Arrange
        var stats = new CouponStatsResult
        {
            CouponId = "coupon_123",
            MaxRedemptions = 100,
            TotalRedemptions = 75
        };

        // Act
        var percentage = stats.UsagePercentage;

        // Assert
        Assert.Equal(75m, percentage);
    }

    [Fact]
    public void CouponStatsResult_UsagePercentage_ReturnsNullWhenNoMax()
    {
        // Arrange
        var stats = new CouponStatsResult
        {
            CouponId = "coupon_123",
            MaxRedemptions = null,
            TotalRedemptions = 75
        };

        // Act
        var percentage = stats.UsagePercentage;

        // Assert
        Assert.Null(percentage);
    }

    [Fact]
    public void CouponStatsResult_UsagePercentage_HandlesZeroMax()
    {
        // Arrange
        var stats = new CouponStatsResult
        {
            CouponId = "coupon_123",
            MaxRedemptions = 0,
            TotalRedemptions = 0
        };

        // Act
        var percentage = stats.UsagePercentage;

        // Assert
        Assert.Null(percentage); // Division by zero protection
    }

    #endregion

    #region CreateCouponRequest Validation Tests

    [Fact]
    public void CreateCouponRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new CreateCouponRequest();

        // Assert
        Assert.Equal(string.Empty, request.Name);
        Assert.Equal("usd", request.Currency);
        Assert.Equal("once", request.Duration);
        Assert.Null(request.PercentOff);
        Assert.Null(request.AmountOffCents);
        Assert.Null(request.MaxRedemptions);
    }

    [Fact]
    public void CreateCouponRequest_LaunchPromotion_HasCorrectValues()
    {
        // Arrange & Act
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
        Assert.Equal("Launch Promotion - 3 Months Free", request.Name);
        Assert.Equal(100m, request.PercentOff);
        Assert.Equal("repeating", request.Duration);
        Assert.Equal(3, request.DurationInMonths);
        Assert.Equal(100, request.MaxRedemptions);
    }

    #endregion

    #region CreatePromoCodeRequest Validation Tests

    [Fact]
    public void CreatePromoCodeRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new CreatePromoCodeRequest();

        // Assert
        Assert.Equal(string.Empty, request.CouponId);
        Assert.False(request.FirstTimeTransactionOnly);
        Assert.True(request.Active);
        Assert.Null(request.Code);
        Assert.Null(request.MaxRedemptions);
    }

    [Fact]
    public void CreatePromoCodeRequest_LaunchPromotion_HasCorrectValues()
    {
        // Arrange & Act
        var request = new CreatePromoCodeRequest
        {
            CouponId = "launch_3mo_free",
            Code = "LAUNCH2024",
            FirstTimeTransactionOnly = true,
            MaxRedemptions = 100
        };

        // Assert
        Assert.Equal("launch_3mo_free", request.CouponId);
        Assert.Equal("LAUNCH2024", request.Code);
        Assert.True(request.FirstTimeTransactionOnly);
        Assert.Equal(100, request.MaxRedemptions);
    }

    #endregion

    #region PromotionStatsResult Tests

    [Fact]
    public void PromotionStatsResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var stats = new PromotionStatsResult();

        // Assert
        Assert.Equal(0, stats.TotalCoupons);
        Assert.Equal(0, stats.ActiveCoupons);
        Assert.Equal(0, stats.TotalPromotionCodes);
        Assert.Equal(0, stats.ActivePromotionCodes);
        Assert.Equal(0, stats.TotalRedemptions);
        Assert.Empty(stats.CouponStats);
    }

    [Fact]
    public void PromotionStatsResult_AggregatesCorrectly()
    {
        // Arrange
        var stats = new PromotionStatsResult
        {
            TotalCoupons = 5,
            ActiveCoupons = 3,
            TotalPromotionCodes = 10,
            ActivePromotionCodes = 7,
            TotalRedemptions = 250,
            CouponStats = new List<CouponStatsResult>
            {
                new() { CouponId = "c1", TotalRedemptions = 100 },
                new() { CouponId = "c2", TotalRedemptions = 150 }
            }
        };

        // Assert
        Assert.Equal(5, stats.TotalCoupons);
        Assert.Equal(3, stats.ActiveCoupons);
        Assert.Equal(2, stats.CouponStats.Count);
        Assert.Equal(250, stats.TotalRedemptions);
    }

    #endregion
}
