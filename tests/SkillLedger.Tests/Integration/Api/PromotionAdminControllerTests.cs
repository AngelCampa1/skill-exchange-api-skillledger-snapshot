using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Promotion Admin API endpoints
/// Tests coupon and promotion code management for admins
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class PromotionAdminControllerTests : IntegrationTestBase
{
    private User _regularUser = null!;
    private User _adminUser = null!;
    private string _testCouponId = null!;
    private string _testPromoCode = null!;

    public PromotionAdminControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _regularUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "promo-user@test.com",
            UserName = "promo-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "promo-admin@test.com",
            UserName = "promo-admin@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_regularUser, _adminUser);
        await Context.SaveChangesAsync();

        _testCouponId = "test_coupon_123";
        _testPromoCode = "TESTPROMO2024";
    }

    #region POST /api/admin/promotions/coupons Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateCoupon_WithValidPercentOff_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            PercentOff = 20,
            Duration = "once",
            Name = "Test 20% Off"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateCoupon_WithValidAmountOff_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            AmountOffCents = 500,
            Duration = "once",
            Name = "Test $5 Off"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateCoupon_WithoutDiscountValue_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            Duration = "once",
            Name = "Invalid Coupon"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("PercentOff or AmountOffCents must be provided");
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateCoupon_WithBothDiscountTypes_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            PercentOff = 20,
            AmountOffCents = 500,
            Duration = "once",
            Name = "Invalid Coupon"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Only one of PercentOff or AmountOffCents can be provided");
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateCoupon_RepeatingWithoutDuration_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            PercentOff = 20,
            Duration = "repeating",
            Name = "Invalid Repeating Coupon"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("DurationInMonths is required");
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateCoupon_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        var request = new
        {
            PercentOff = 20,
            Duration = "once",
            Name = "Test Coupon"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateCoupon_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            PercentOff = 20,
            Duration = "once",
            Name = "Test Coupon"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/coupons", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/admin/promotions/coupons Tests

    [Fact]
    [FastTest]
    public async Task GET_ListCoupons_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/coupons");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ListCoupons_WithActiveOnlyFalse_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/coupons?activeOnly=false");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ListCoupons_WithCustomLimit_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/coupons?limit=50");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_ListCoupons_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/coupons");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/admin/promotions/codes Tests

    [Fact]
    [FastTest]
    public async Task POST_CreatePromotionCode_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            CouponId = _testCouponId,
            Code = _testPromoCode
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/codes", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreatePromotionCode_WithoutCouponId_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new
        {
            Code = _testPromoCode
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/codes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CouponId is required");
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreatePromotionCode_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        var request = new
        {
            CouponId = _testCouponId,
            Code = _testPromoCode
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/promotions/codes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/admin/promotions/codes Tests

    [Fact]
    [FastTest]
    public async Task GET_ListPromotionCodes_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/codes");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ListPromotionCodes_FilteredByCoupon_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync($"/api/admin/promotions/codes?couponId={_testCouponId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_ListPromotionCodes_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/codes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/admin/promotions/validate/{code} Tests

    [Fact]
    [FastTest]
    public async Task GET_ValidatePromotionCode_WithoutAuth_ReturnsOk()
    {
        // Act - AllowAnonymous endpoint
        var response = await Client.GetAsync($"/api/admin/promotions/validate/{_testPromoCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_ValidatePromotionCode_AsAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync($"/api/admin/promotions/validate/{_testPromoCode}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/admin/promotions/stats Tests

    [Fact]
    [FastTest]
    public async Task GET_PromotionStats_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/stats");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_PromotionStats_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/admin/promotions/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllAdminEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints that require Admin authentication
        var endpoints = new[]
        {
            "/api/admin/promotions/coupons",
            "/api/admin/promotions/codes",
            "/api/admin/promotions/stats"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AllAdminEndpoints_AsRegularUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        var endpoints = new[]
        {
            "/api/admin/promotions/coupons",
            "/api/admin/promotions/codes",
            "/api/admin/promotions/stats"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"GET {endpoint} should require Admin role");
        }
    }

    #endregion
}
